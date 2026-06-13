package main

import (
	"crypto/rand"
	"database/sql"
	"errors"
	"fmt"
	"strings"
	"time"

	_ "modernc.org/sqlite" // pure-Go driver: static binary, scratch-container friendly
)

type Store struct{ db *sql.DB }

func OpenStore(path string) (*Store, error) {
	db, err := sql.Open("sqlite", path+"?_pragma=journal_mode(WAL)&_pragma=busy_timeout(5000)")
	if err != nil {
		return nil, err
	}
	db.SetMaxOpenConns(1) // sqlite: serialize writers, avoids SQLITE_BUSY entirely
	_, err = db.Exec(`
CREATE TABLE IF NOT EXISTS users (
  uid        TEXT PRIMARY KEY,
  username   TEXT NOT NULL UNIQUE,
  elo        INTEGER NOT NULL DEFAULT 1000,
  created_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS friendships (
  a            TEXT NOT NULL,           -- uid min(a,b): one row per pair
  b            TEXT NOT NULL,
  status       TEXT NOT NULL,           -- 'pending' | 'accepted'
  requested_by TEXT NOT NULL,
  created_at   INTEGER NOT NULL,
  PRIMARY KEY (a, b)
);
CREATE TABLE IF NOT EXISTS devices (
  token      TEXT PRIMARY KEY,
  uid        TEXT NOT NULL,
  platform   TEXT NOT NULL DEFAULT '',
  updated_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS saves (
  uid        TEXT PRIMARY KEY,        -- one cloud save blob per user (collection/coins/team/…)
  rev        INTEGER NOT NULL,        -- client's monotonic revision; last-write-wins
  data       TEXT NOT NULL,           -- opaque PlayerProfile JSON
  updated_at INTEGER NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_users_elo ON users (elo DESC);
CREATE INDEX IF NOT EXISTS idx_devices_uid ON devices (uid);`)
	if err != nil {
		return nil, err
	}
	return &Store{db: db}, nil
}

type User struct {
	Uid      string `json:"uid"`
	Username string `json:"username"`
	Elo      int    `json:"elo"`
	Rank     int    `json:"rank,omitempty"`
}

var ErrUsernameTaken = errors.New("username taken")

// EnsureUser returns the existing user, or creates one with an auto-generated unique username
// (e.g. "Trainer4821"). Never fails on a name collision — used by the launch-time profile sync,
// which must always succeed so the player can play. Existing users keep their chosen name.
func (s *Store) EnsureUser(uid string) (User, error) {
	if u, err := s.GetUser(uid); err == nil {
		return u, nil // already exists — don't touch the username
	} else if err != sql.ErrNoRows {
		return User{}, err
	}
	now := time.Now().Unix()
	for attempt := 0; attempt < 20; attempt++ {
		name := randomUsername()
		_, err := s.db.Exec(`INSERT INTO users (uid, username, elo, created_at) VALUES (?, ?, 1000, ?)`,
			uid, name, now)
		if err == nil {
			return s.GetUser(uid)
		}
		if !isUniqueErr(err) {
			return User{}, err
		}
		if u, e := s.GetUser(uid); e == nil {
			return u, nil // uid created concurrently
		}
		// else: name collided — loop and try another
	}
	return User{}, errors.New("could not allocate a username")
}

// SetUsername is the EXPLICIT rename (username editor): strictly unique, errors if taken.
func (s *Store) SetUsername(uid, username string) (User, error) {
	res, err := s.db.Exec(`UPDATE users SET username = ? WHERE uid = ?`, username, uid)
	if err != nil {
		if isUniqueErr(err) {
			return User{}, ErrUsernameTaken
		}
		return User{}, err
	}
	if n, _ := res.RowsAffected(); n == 0 {
		return User{}, sql.ErrNoRows // no such user yet
	}
	return s.GetUser(uid)
}

func randomUsername() string {
	var b [3]byte
	_, _ = rand.Read(b[:])
	n := (int(b[0])<<16 | int(b[1])<<8 | int(b[2])) % 10000
	return fmt.Sprintf("Trainer%04d", n)
}

func isUniqueErr(err error) bool {
	return err != nil && strings.Contains(err.Error(), "constraint failed")
}

func (s *Store) GetUser(uid string) (User, error) {
	var u User
	err := s.db.QueryRow(`SELECT uid, username, elo,
		(SELECT COUNT(*) + 1 FROM users x WHERE x.elo > users.elo) AS rank
		FROM users WHERE uid = ?`, uid).Scan(&u.Uid, &u.Username, &u.Elo, &u.Rank)
	return u, err
}

func (s *Store) GetUserByName(username string) (User, error) {
	var u User
	err := s.db.QueryRow(`SELECT uid, username, elo FROM users WHERE username = ?`, username).
		Scan(&u.Uid, &u.Username, &u.Elo)
	return u, err
}

func (s *Store) TopUsers(limit int) ([]User, error) {
	rows, err := s.db.Query(`SELECT uid, username, elo FROM users ORDER BY elo DESC, created_at ASC LIMIT ?`, limit)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var out []User
	rank := 0
	for rows.Next() {
		var u User
		if err := rows.Scan(&u.Uid, &u.Username, &u.Elo); err != nil {
			return nil, err
		}
		rank++
		u.Rank = rank
		out = append(out, u)
	}
	return out, rows.Err()
}

func (s *Store) SetElo(uid string, elo int) error {
	_, err := s.db.Exec(`UPDATE users SET elo = ? WHERE uid = ?`, elo, uid)
	return err
}

// ---- friendships ------------------------------------------------------------------------------

func pairKey(u1, u2 string) (string, string) {
	if u1 < u2 {
		return u1, u2
	}
	return u2, u1
}

func (s *Store) RequestFriend(from, to string) error {
	a, b := pairKey(from, to)
	_, err := s.db.Exec(`INSERT INTO friendships (a, b, status, requested_by, created_at)
		VALUES (?, ?, 'pending', ?, ?)
		ON CONFLICT(a, b) DO NOTHING`, a, b, from, time.Now().Unix())
	return err
}

func (s *Store) RespondFriend(me, other string, accept bool) error {
	a, b := pairKey(me, other)
	if accept {
		// Only the NON-requester may accept.
		res, err := s.db.Exec(`UPDATE friendships SET status = 'accepted'
			WHERE a = ? AND b = ? AND status = 'pending' AND requested_by != ?`, a, b, me)
		if err != nil {
			return err
		}
		if n, _ := res.RowsAffected(); n == 0 {
			return errors.New("no pending request")
		}
		return nil
	}
	_, err := s.db.Exec(`DELETE FROM friendships WHERE a = ? AND b = ?`, a, b)
	return err
}

func (s *Store) RemoveFriend(me, other string) error {
	a, b := pairKey(me, other)
	_, err := s.db.Exec(`DELETE FROM friendships WHERE a = ? AND b = ?`, a, b)
	return err
}

type Friend struct {
	User
	Status    string `json:"status"`             // pending | accepted
	Direction string `json:"direction,omitempty"` // incoming | outgoing (pending only)
}

func (s *Store) Friends(me string) ([]Friend, error) {
	rows, err := s.db.Query(`
		SELECT u.uid, u.username, u.elo, f.status, f.requested_by
		FROM friendships f
		JOIN users u ON u.uid = CASE WHEN f.a = ? THEN f.b ELSE f.a END
		WHERE f.a = ? OR f.b = ?
		ORDER BY f.status DESC, u.elo DESC`, me, me, me)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var out []Friend
	for rows.Next() {
		var fr Friend
		var requestedBy string
		if err := rows.Scan(&fr.Uid, &fr.Username, &fr.Elo, &fr.Status, &requestedBy); err != nil {
			return nil, err
		}
		if fr.Status == "pending" {
			if requestedBy == me {
				fr.Direction = "outgoing"
			} else {
				fr.Direction = "incoming"
			}
		}
		out = append(out, fr)
	}
	return out, rows.Err()
}

// ---- devices ----------------------------------------------------------------------------------

func (s *Store) RegisterDevice(uid, token, platform string) error {
	_, err := s.db.Exec(`INSERT INTO devices (token, uid, platform, updated_at) VALUES (?, ?, ?, ?)
		ON CONFLICT(token) DO UPDATE SET uid = excluded.uid, platform = excluded.platform, updated_at = excluded.updated_at`,
		token, uid, platform, time.Now().Unix())
	return err
}

// ---- cloud save (collection blob) -------------------------------------------------------------

// GetSave returns the stored rev + blob (rev 0, "" when none).
func (s *Store) GetSave(uid string) (int, string, error) {
	var rev int
	var data string
	err := s.db.QueryRow(`SELECT rev, data FROM saves WHERE uid = ?`, uid).Scan(&rev, &data)
	if err == sql.ErrNoRows {
		return 0, "", nil
	}
	return rev, data, err
}

// PutSave stores the blob only if rev is newer (last-write-wins). Returns the resulting rev.
func (s *Store) PutSave(uid string, rev int, data string) (int, error) {
	cur, _, err := s.GetSave(uid)
	if err != nil {
		return 0, err
	}
	if rev < cur {
		return cur, nil // stale write — keep the newer cloud copy
	}
	_, err = s.db.Exec(`INSERT INTO saves (uid, rev, data, updated_at) VALUES (?, ?, ?, ?)
		ON CONFLICT(uid) DO UPDATE SET rev = excluded.rev, data = excluded.data, updated_at = excluded.updated_at`,
		uid, rev, data, time.Now().Unix())
	return rev, err
}

func (s *Store) DeviceTokens(uid string) ([]string, error) {
	rows, err := s.db.Query(`SELECT token FROM devices WHERE uid = ?`, uid)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var out []string
	for rows.Next() {
		var t string
		if err := rows.Scan(&t); err != nil {
			return nil, err
		}
		out = append(out, t)
	}
	return out, rows.Err()
}

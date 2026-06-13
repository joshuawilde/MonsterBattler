package main

import (
	"context"
	"crypto/subtle"
	"encoding/json"
	"log"
	"net/http"
	"strings"
	"time"

	fbauth "firebase.google.com/go/v4/auth"
	"firebase.google.com/go/v4/messaging"
)

type Server struct {
	Store       *Store
	Auth        *fbauth.Client
	Push        *messaging.Client
	Match       *Matchmaker
	Presence    *Presence
	InternalKey string // shared secret for the battle server's result reports
}

func writeJSON(w http.ResponseWriter, code int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(code)
	_ = json.NewEncoder(w).Encode(v)
}

func readJSON(w http.ResponseWriter, r *http.Request, v any) bool {
	if err := json.NewDecoder(http.MaxBytesReader(w, r.Body, 1<<16)).Decode(v); err != nil {
		http.Error(w, "bad json", http.StatusBadRequest)
		return false
	}
	return true
}

// POST /v1/profile/sync → full profile. Creates the account with an auto-generated unique
// username on first call (the client adopts whatever name comes back). Never 409s.
func (s *Server) ProfileSync(w http.ResponseWriter, r *http.Request, uid string) {
	u, err := s.Store.EnsureUser(uid)
	if err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusOK, u)
}

// POST /v1/profile/username {username} → explicit rename; 409 if taken.
func (s *Server) SetUsername(w http.ResponseWriter, r *http.Request, uid string) {
	var in struct{ Username string `json:"username"` }
	if !readJSON(w, r, &in) {
		return
	}
	in.Username = strings.TrimSpace(in.Username)
	if len(in.Username) < 2 || len(in.Username) > 24 {
		http.Error(w, "username must be 2-24 chars", http.StatusBadRequest)
		return
	}
	u, err := s.Store.SetUsername(uid, in.Username)
	if err == ErrUsernameTaken {
		http.Error(w, "username taken", http.StatusConflict)
		return
	}
	if err != nil {
		http.Error(w, "sync your profile first", http.StatusBadRequest)
		return
	}
	writeJSON(w, http.StatusOK, u)
}

// GET /v1/leaderboard?limit=50 → {top: [...], me: {...}}
func (s *Server) Leaderboard(w http.ResponseWriter, r *http.Request, uid string) {
	limit := 50
	if l := r.URL.Query().Get("limit"); l != "" {
		if _, err := json.Number(l).Int64(); err == nil {
			if n, _ := json.Number(l).Int64(); n >= 1 && n <= 200 {
				limit = int(n)
			}
		}
	}
	top, err := s.Store.TopUsers(limit)
	if err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	me, _ := s.Store.GetUser(uid) // zero-value if not synced yet
	writeJSON(w, http.StatusOK, map[string]any{"top": top, "me": me})
}

// GET /v1/friends
func (s *Server) FriendsList(w http.ResponseWriter, r *http.Request, uid string) {
	friends, err := s.Store.Friends(uid)
	if err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	if friends == nil {
		friends = []Friend{}
	}
	writeJSON(w, http.StatusOK, map[string]any{"friends": friends})
}

// POST /v1/friends/request {username}
func (s *Server) FriendRequest(w http.ResponseWriter, r *http.Request, uid string) {
	var in struct{ Username string `json:"username"` }
	if !readJSON(w, r, &in) {
		return
	}
	target, err := s.Store.GetUserByName(strings.TrimSpace(in.Username))
	if err != nil {
		http.Error(w, "no such user", http.StatusNotFound)
		return
	}
	if target.Uid == uid {
		http.Error(w, "that's you", http.StatusBadRequest)
		return
	}
	if err := s.Store.RequestFriend(uid, target.Uid); err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	if me, err := s.Store.GetUser(uid); err == nil {
		s.notify(target.Uid, "Friend request", me.Username+" wants to be your friend!")
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true})
}

// POST /v1/friends/respond {uid, accept}
func (s *Server) FriendRespond(w http.ResponseWriter, r *http.Request, uid string) {
	var in struct {
		Uid    string `json:"uid"`
		Accept bool   `json:"accept"`
	}
	if !readJSON(w, r, &in) {
		return
	}
	if err := s.Store.RespondFriend(uid, in.Uid, in.Accept); err != nil {
		http.Error(w, err.Error(), http.StatusBadRequest)
		return
	}
	if in.Accept {
		if me, err := s.Store.GetUser(uid); err == nil {
			s.notify(in.Uid, "Friend request accepted", me.Username+" accepted your friend request!")
		}
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true})
}

// DELETE /v1/friends/{uid}
func (s *Server) FriendRemove(w http.ResponseWriter, r *http.Request, uid string) {
	other := r.PathValue("uid")
	if other == "" {
		http.Error(w, "missing uid", http.StatusBadRequest)
		return
	}
	if err := s.Store.RemoveFriend(uid, other); err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true})
}

// POST /v1/devices {token, platform}
func (s *Server) RegisterDevice(w http.ResponseWriter, r *http.Request, uid string) {
	var in struct {
		Token    string `json:"token"`
		Platform string `json:"platform"`
	}
	if !readJSON(w, r, &in) || in.Token == "" {
		if in.Token == "" {
			http.Error(w, "missing token", http.StatusBadRequest)
		}
		return
	}
	if err := s.Store.RegisterDevice(uid, in.Token, in.Platform); err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"ok": true})
}

// GET /v1/save → {rev, data} (the player's cloud collection blob; rev 0 / "" when none).
func (s *Server) GetSave(w http.ResponseWriter, r *http.Request, uid string) {
	rev, data, err := s.Store.GetSave(uid)
	if err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"rev": rev, "data": data})
}

// PUT /v1/save {rev, data} → {rev} (stored only if newer; returns the winning rev).
func (s *Server) PutSave(w http.ResponseWriter, r *http.Request, uid string) {
	var in struct {
		Rev  int    `json:"rev"`
		Data string `json:"data"`
	}
	if !readJSON(w, r, &in) {
		return
	}
	if len(in.Data) > 1<<20 { // 1MB cap — a save blob is a few KB
		http.Error(w, "save too large", http.StatusBadRequest)
		return
	}
	rev, err := s.Store.PutSave(uid, in.Rev, in.Data)
	if err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{"rev": rev})
}

// GET /v1/online → {count} of players seen recently (includes the caller, who is marked online).
func (s *Server) Online(w http.ResponseWriter, r *http.Request, uid string) {
	writeJSON(w, http.StatusOK, map[string]any{"count": s.Presence.Touch(uid, time.Now())})
}

// POST /v1/match/queue → enqueue + return current status (same shape as /v1/match/status)
func (s *Server) MatchQueue(w http.ResponseWriter, r *http.Request, uid string) {
	u, err := s.Store.GetUser(uid)
	if err != nil {
		http.Error(w, "sync your profile first", http.StatusBadRequest)
		return
	}
	s.Match.Enqueue(uid, u.Username, u.Elo)
	s.writeMatchStatus(w, uid)
}

// GET /v1/match/status → {state, host, port, opponent?} — clients poll this after queuing.
func (s *Server) MatchStatus(w http.ResponseWriter, r *http.Request, uid string) {
	s.writeMatchStatus(w, uid)
}

// POST /v1/match/cancel → leave the queue (only effective while still waiting)
func (s *Server) MatchCancel(w http.ResponseWriter, r *http.Request, uid string) {
	s.Match.Cancel(uid)
	writeJSON(w, http.StatusOK, map[string]any{"state": "idle"})
}

func (s *Server) writeMatchStatus(w http.ResponseWriter, uid string) {
	m := s.Match.Status(uid)
	if m == nil {
		writeJSON(w, http.StatusOK, map[string]any{"state": "idle"})
		return
	}
	resp := map[string]any{}
	switch m.State {
	case stateWaiting:
		resp["state"] = "searching"
	case stateError:
		resp["state"] = "error"
		resp["error"] = m.Err
	case stateReady:
		resp["state"] = "ready"
		resp["wsUrl"] = m.WsURL
		resp["matchId"] = m.ID
		// who's the opponent + which sim side is this caller (server's canonical join order)
		oppUid := m.UID1
		resp["side"] = 0
		if uid == m.UID1 {
			oppUid, resp["side"] = m.UID0, 1
		}
		if opp, err := s.Store.GetUser(oppUid); err == nil {
			resp["opponent"] = opp.Username
			resp["opponentElo"] = opp.Elo
		}
	}
	writeJSON(w, http.StatusOK, resp)
}

// POST /v1/internal/match-result {uid0, uid1, winnerSide} — battle server only (X-Api-Key).
// winnerSide: 0, 1, or -1 for a draw. Elo is computed HERE so clients can't forge ratings.
func (s *Server) MatchResult(w http.ResponseWriter, r *http.Request) {
	if s.InternalKey == "" ||
		subtle.ConstantTimeCompare([]byte(r.Header.Get("X-Api-Key")), []byte(s.InternalKey)) != 1 {
		http.Error(w, "forbidden", http.StatusForbidden)
		return
	}
	var in struct {
		Uid0       string `json:"uid0"`
		Uid1       string `json:"uid1"`
		WinnerSide int    `json:"winnerSide"`
	}
	if !readJSON(w, r, &in) {
		return
	}
	u0, err0 := s.Store.GetUser(in.Uid0)
	u1, err1 := s.Store.GetUser(in.Uid1)
	if err0 != nil || err1 != nil {
		http.Error(w, "unknown player", http.StatusNotFound)
		return
	}
	var score0 float64
	switch in.WinnerSide {
	case 0:
		score0 = 1
	case 1:
		score0 = 0
	default:
		score0 = 0.5
	}
	newElo0, newElo1 := EloUpdate(u0.Elo, u1.Elo, score0)
	if err := s.Store.SetElo(u0.Uid, newElo0); err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	if err := s.Store.SetElo(u1.Uid, newElo1); err != nil {
		http.Error(w, "server error", http.StatusInternalServerError)
		return
	}
	log.Printf("match: %s(%d→%d) vs %s(%d→%d) winner=%d",
		u0.Username, u0.Elo, newElo0, u1.Username, u1.Elo, newElo1, in.WinnerSide)
	if s.Match != nil {
		s.Match.Finish(in.Uid0, in.Uid1) // destroy the per-match actor
	}
	writeJSON(w, http.StatusOK, map[string]any{"elo0": newElo0, "elo1": newElo1})
}

// notify sends a push to all of a user's devices; quietly does nothing in dev-bypass mode.
func (s *Server) notify(uid, title, body string) {
	if s.Push == nil {
		return
	}
	tokens, err := s.Store.DeviceTokens(uid)
	if err != nil || len(tokens) == 0 {
		return
	}
	_, err = s.Push.SendEachForMulticast(context.Background(), &messaging.MulticastMessage{
		Tokens:       tokens,
		Notification: &messaging.Notification{Title: title, Body: body},
	})
	if err != nil {
		log.Printf("push to %s failed: %v", uid, err)
	}
}

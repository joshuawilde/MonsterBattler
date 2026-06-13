package main

import (
	"context"
	"log"
	"sync"
	"time"
)

// Matchmaker: an in-memory queue paired by Elo proximity, plus the per-match Rivet actor
// lifecycle. State is process-local (single backend instance) — fine at this scale; move to
// Redis if the backend ever needs to scale horizontally.
type Matchmaker struct {
	store   *Store
	servers *BattleServers

	mu      sync.Mutex
	queue   []*waiter         // players waiting for an opponent
	matches map[string]*Match // matchID → match (also keyed per-uid via uidMatch)
	uid2mid map[string]string // uid → matchID (so status lookups are O(1))
}

type waiter struct {
	uid      string
	username string
	elo      int
	since    time.Time
}

type matchState int

const (
	stateWaiting matchState = iota // in queue, no opponent yet
	stateReady                     // actor up, host/port assigned
	stateError                     // spawn failed
)

type Match struct {
	ID      string
	UID0    string
	UID1    string
	WsURL   string
	State   matchState
	Err     string
	Created time.Time
}

func NewMatchmaker(store *Store, servers *BattleServers) *Matchmaker {
	m := &Matchmaker{
		store:   store,
		servers: servers,
		matches: map[string]*Match{},
		uid2mid: map[string]string{},
	}
	go m.sweep()
	return m
}

// Enqueue adds the player to the queue (idempotent) and pairs the two closest-Elo waiters.
// Returns the matchID once this player is in/assigned to a match, else "".
func (m *Matchmaker) Enqueue(uid, username string, elo int) string {
	m.mu.Lock()
	defer m.mu.Unlock()

	if mid, ok := m.uid2mid[uid]; ok {
		return mid // already queued or matched
	}
	// already waiting? refresh and bail.
	for _, w := range m.queue {
		if w.uid == uid {
			return ""
		}
	}
	m.queue = append(m.queue, &waiter{uid: uid, username: username, elo: elo, since: time.Now()})
	m.tryPair()
	return m.uid2mid[uid]
}

// tryPair pairs the two waiters with the smallest Elo gap and spawns their actor.
// Caller holds the lock. Spawning happens in a goroutine so the HTTP call returns fast.
func (m *Matchmaker) tryPair() {
	for len(m.queue) >= 2 {
		// closest Elo pair among waiters
		bi, bj, best := 0, 1, 1<<30
		for i := 0; i < len(m.queue); i++ {
			for j := i + 1; j < len(m.queue); j++ {
				d := m.queue[i].elo - m.queue[j].elo
				if d < 0 {
					d = -d
				}
				if d < best {
					bi, bj, best = i, j, d
				}
			}
		}
		w0, w1 := m.queue[bi], m.queue[bj]
		// remove both (higher index first)
		m.queue = removeAt(removeAt(m.queue, bj), bi)

		mid := newMatchID()
		match := &Match{ID: mid, UID0: w0.uid, UID1: w1.uid, State: stateWaiting, Created: time.Now()}
		m.matches[mid] = match
		m.uid2mid[w0.uid] = mid
		m.uid2mid[w1.uid] = mid
		log.Printf("match %s: %s(%d) vs %s(%d), registering on battle server", mid, w0.username, w0.elo, w1.username, w1.elo)
		go m.register(match)
	}
}

func (m *Matchmaker) register(match *Match) {
	ctx, cancel := context.WithTimeout(context.Background(), 15*time.Second)
	defer cancel()
	wsURL, err := m.servers.Register(ctx, match.ID, match.UID0, match.UID1)
	m.mu.Lock()
	defer m.mu.Unlock()
	if err != nil {
		log.Printf("match %s: register failed: %v", match.ID, err)
		match.State, match.Err = stateError, "couldn't start a server"
		return
	}
	match.WsURL, match.State = wsURL, stateReady
	log.Printf("match %s: ready at %s", match.ID, wsURL)
}

// Status returns this player's current match, the sentinel waitingMatch if they're queued but
// unpaired, or nil if they're not in the system at all.
func (m *Matchmaker) Status(uid string) *Match {
	m.mu.Lock()
	defer m.mu.Unlock()
	if mid, ok := m.uid2mid[uid]; ok {
		return m.matches[mid]
	}
	for _, w := range m.queue {
		if w.uid == uid {
			return waitingMatch
		}
	}
	return nil
}

// waitingMatch is a shared sentinel meaning "queued, no opponent yet".
var waitingMatch = &Match{State: stateWaiting}

// Cancel removes a still-waiting player from the queue.
func (m *Matchmaker) Cancel(uid string) {
	m.mu.Lock()
	defer m.mu.Unlock()
	for i, w := range m.queue {
		if w.uid == uid {
			m.queue = removeAt(m.queue, i)
			return
		}
	}
}

// Finish clears the match's bookkeeping when the result report arrives. The battle server frees
// the match itself on disconnect, so there's no actor to tear down — just forget it here.
func (m *Matchmaker) Finish(uid0, uid1 string) {
	m.mu.Lock()
	defer m.mu.Unlock()
	mid := m.uid2mid[uid0]
	if match := m.matches[mid]; match != nil {
		delete(m.matches, mid)
		delete(m.uid2mid, match.UID0)
		delete(m.uid2mid, match.UID1)
	}
}

// sweep forgets matches older than the TTL so a never-finished match (both players vanished
// before a result) doesn't pin its players in "ready" forever.
func (m *Matchmaker) sweep() {
	const ttl = 30 * time.Minute
	for range time.Tick(2 * time.Minute) {
		m.mu.Lock()
		for _, match := range m.matches {
			if time.Since(match.Created) > ttl {
				delete(m.matches, match.ID)
				delete(m.uid2mid, match.UID0)
				delete(m.uid2mid, match.UID1)
				log.Printf("match %s: swept (stale)", match.ID)
			}
		}
		m.mu.Unlock()
	}
}

func removeAt[T any](s []*T, i int) []*T {
	return append(s[:i], s[i+1:]...)
}

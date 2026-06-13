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
	store *Store
	rivet *Rivet

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
	ActorID string
	Host    string
	Port    int
	State   matchState
	Err     string
	Created time.Time
}

func NewMatchmaker(store *Store, rivet *Rivet) *Matchmaker {
	m := &Matchmaker{
		store:   store,
		rivet:   rivet,
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
		log.Printf("match %s: %s(%d) vs %s(%d), spawning actor", mid, w0.username, w0.elo, w1.username, w1.elo)
		go m.spawn(match)
	}
}

func (m *Matchmaker) spawn(match *Match) {
	ctx, cancel := context.WithTimeout(context.Background(), 60*time.Second)
	defer cancel()
	actor, err := m.rivet.Create(ctx, match.ID)
	m.mu.Lock()
	defer m.mu.Unlock()
	if err != nil {
		log.Printf("match %s: spawn failed: %v", match.ID, err)
		match.State, match.Err = stateError, "couldn't start a server"
		return
	}
	match.ActorID, match.Host, match.Port, match.State = actor.ID, actor.Host, actor.Port, stateReady
	log.Printf("match %s: ready at %s:%d (actor %s)", match.ID, actor.Host, actor.Port, actor.ID)
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

// Finish tears down the match's actor and clears its bookkeeping. Called when the result
// report arrives (and by the sweeper for stragglers).
func (m *Matchmaker) Finish(uid0, uid1 string) {
	m.mu.Lock()
	mid := m.uid2mid[uid0]
	match := m.matches[mid]
	if match != nil {
		delete(m.matches, mid)
		delete(m.uid2mid, match.UID0)
		delete(m.uid2mid, match.UID1)
	}
	m.mu.Unlock()
	if match != nil && match.ActorID != "" {
		ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
		defer cancel()
		if err := m.rivet.Destroy(ctx, match.ActorID); err != nil {
			log.Printf("match %s: destroy actor %s failed: %v", mid, match.ActorID, err)
		} else {
			log.Printf("match %s: actor %s destroyed", mid, match.ActorID)
		}
	}
}

// sweep destroys actors for matches older than the TTL — covers crashes, double-disconnects,
// and abandoned lobbies so nothing leaks (and bills) forever.
func (m *Matchmaker) sweep() {
	const ttl = 30 * time.Minute
	for range time.Tick(2 * time.Minute) {
		m.mu.Lock()
		var stale []*Match
		for _, match := range m.matches {
			if time.Since(match.Created) > ttl {
				stale = append(stale, match)
			}
		}
		for _, match := range stale {
			delete(m.matches, match.ID)
			delete(m.uid2mid, match.UID0)
			delete(m.uid2mid, match.UID1)
		}
		m.mu.Unlock()
		for _, match := range stale {
			log.Printf("match %s: sweeping stale actor %s", match.ID, match.ActorID)
			ctx, cancel := context.WithTimeout(context.Background(), 30*time.Second)
			_ = m.rivet.Destroy(ctx, match.ActorID)
			cancel()
		}
	}
}

func removeAt[T any](s []*T, i int) []*T {
	return append(s[:i], s[i+1:]...)
}

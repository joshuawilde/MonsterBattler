package main

import (
	"sync"
	"time"
)

// Presence tracks who's online by last-seen heartbeat. The home screen polls /v1/online, which
// both refreshes the caller's timestamp and returns the count of players seen within the window
// (so the count includes the caller).
type Presence struct {
	mu   sync.Mutex
	seen map[string]time.Time
}

const presenceWindow = 45 * time.Second

func NewPresence() *Presence { return &Presence{seen: map[string]time.Time{}} }

// Touch marks uid online (using injectable now for tests) and returns the live count.
func (p *Presence) Touch(uid string, now time.Time) int {
	p.mu.Lock()
	defer p.mu.Unlock()
	p.seen[uid] = now
	count := 0
	for u, t := range p.seen {
		if now.Sub(t) <= presenceWindow {
			count++
		} else {
			delete(p.seen, u) // prune stale entries while we're here
		}
	}
	return count
}

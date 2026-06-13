package main

import (
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"testing"
	"time"
)

func newMatchTestServer(t *testing.T) *httptest.Server {
	t.Helper()
	t.Setenv("AUTH_DEV_BYPASS", "1")
	t.Setenv("RIVET_STUB_HOST", "10.0.0.5:7777")
	store, err := OpenStore(filepath.Join(t.TempDir(), "m.db"))
	if err != nil {
		t.Fatal(err)
	}
	s := &Server{Store: store, InternalKey: "k", Match: NewMatchmaker(store, NewRivet())}
	mux := http.NewServeMux()
	mux.Handle("POST /v1/profile/sync", s.WithAuth(s.ProfileSync))
	mux.Handle("POST /v1/match/queue", s.WithAuth(s.MatchQueue))
	mux.Handle("GET /v1/match/status", s.WithAuth(s.MatchStatus))
	mux.HandleFunc("POST /v1/internal/match-result", s.MatchResult)
	ts := httptest.NewServer(mux)
	t.Cleanup(ts.Close)
	return ts
}

func TestMatchmakingStubFlow(t *testing.T) {
	ts := newMatchTestServer(t)
	call(t, ts, "POST", "/v1/profile/sync", "p1", `{"username":"One"}`, nil)
	call(t, ts, "POST", "/v1/profile/sync", "p2", `{"username":"Two"}`, nil)

	// p1 queues alone → searching
	_, b1 := call(t, ts, "POST", "/v1/match/queue", "p1", `{}`, nil)
	if b1["state"] != "searching" {
		t.Fatalf("p1 first queue: %v", b1)
	}
	// p2 queues → both paired, actor spawns (stub via goroutine)
	call(t, ts, "POST", "/v1/match/queue", "p2", `{}`, nil)

	ready := pollReady(t, ts, "p1")
	if ready["host"] != "10.0.0.5" || int(ready["port"].(float64)) != 7777 {
		t.Fatalf("p1 endpoint: %v", ready)
	}
	if ready["opponent"] != "Two" || int(ready["side"].(float64)) != 0 {
		t.Fatalf("p1 opponent/side: %v", ready)
	}
	// p2 sees the same actor, side 1, opponent One
	r2 := pollReady(t, ts, "p2")
	if r2["host"] != "10.0.0.5" || r2["opponent"] != "One" || int(r2["side"].(float64)) != 1 {
		t.Fatalf("p2 view: %v", r2)
	}

	// result report ends the match → status returns to idle (actor "destroyed")
	call(t, ts, "POST", "/v1/internal/match-result", "",
		`{"uid0":"p1","uid1":"p2","winnerSide":0}`, map[string]string{"X-Api-Key": "k"})
	_, after := call(t, ts, "GET", "/v1/match/status", "p1", "", nil)
	if after["state"] != "idle" {
		t.Fatalf("after result, expected idle: %v", after)
	}
}

func pollReady(t *testing.T, ts *httptest.Server, uid string) map[string]any {
	t.Helper()
	for i := 0; i < 50; i++ {
		_, b := call(t, ts, "GET", "/v1/match/status", uid, "", nil)
		if b["state"] == "ready" {
			return b
		}
		if b["state"] == "error" {
			t.Fatalf("%s match errored: %v", uid, b)
		}
		time.Sleep(20 * time.Millisecond)
	}
	t.Fatalf("%s never became ready", uid)
	return nil
}

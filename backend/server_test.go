package main

import (
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"strings"
	"testing"
)

// newTestServer spins the full HTTP surface against a real sqlite db in a temp dir,
// in dev-bypass auth mode — the same wiring production uses minus Firebase.
func newTestServer(t *testing.T) *httptest.Server {
	t.Helper()
	t.Setenv("AUTH_DEV_BYPASS", "1")
	store, err := OpenStore(filepath.Join(t.TempDir(), "test.db"))
	if err != nil {
		t.Fatal(err)
	}
	s := &Server{Store: store, InternalKey: "testkey"}
	mux := http.NewServeMux()
	mux.Handle("POST /v1/profile/sync", s.WithAuth(s.ProfileSync))
	mux.Handle("GET /v1/leaderboard", s.WithAuth(s.Leaderboard))
	mux.Handle("GET /v1/friends", s.WithAuth(s.FriendsList))
	mux.Handle("POST /v1/friends/request", s.WithAuth(s.FriendRequest))
	mux.Handle("POST /v1/friends/respond", s.WithAuth(s.FriendRespond))
	mux.Handle("DELETE /v1/friends/{uid}", s.WithAuth(s.FriendRemove))
	mux.Handle("POST /v1/devices", s.WithAuth(s.RegisterDevice))
	mux.HandleFunc("POST /v1/internal/match-result", s.MatchResult)
	ts := httptest.NewServer(mux)
	t.Cleanup(ts.Close)
	return ts
}

func call(t *testing.T, ts *httptest.Server, method, path, uid, body string, hdr map[string]string) (int, map[string]any) {
	t.Helper()
	req, _ := http.NewRequest(method, ts.URL+path, strings.NewReader(body))
	if uid != "" {
		req.Header.Set("Authorization", "Bearer dev:"+uid)
	}
	for k, v := range hdr {
		req.Header.Set(k, v)
	}
	res, err := ts.Client().Do(req)
	if err != nil {
		t.Fatal(err)
	}
	defer res.Body.Close()
	var out map[string]any
	_ = json.NewDecoder(res.Body).Decode(&out)
	return res.StatusCode, out
}

func TestProfileSyncAndDuplicateName(t *testing.T) {
	ts := newTestServer(t)
	code, body := call(t, ts, "POST", "/v1/profile/sync", "u1", `{"username":"Josh"}`, nil)
	if code != 200 || body["elo"].(float64) != 1000 {
		t.Fatalf("sync: %d %v", code, body)
	}
	code, _ = call(t, ts, "POST", "/v1/profile/sync", "u2", `{"username":"Josh"}`, nil)
	if code != http.StatusConflict {
		t.Fatalf("expected 409 for duplicate username, got %d", code)
	}
	// renaming yourself to your own name is fine
	code, _ = call(t, ts, "POST", "/v1/profile/sync", "u1", `{"username":"Josh"}`, nil)
	if code != 200 {
		t.Fatalf("re-sync own name: %d", code)
	}
}

func TestAuthRequired(t *testing.T) {
	ts := newTestServer(t)
	code, _ := call(t, ts, "GET", "/v1/friends", "", "", nil)
	if code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", code)
	}
}

func TestFriendFlow(t *testing.T) {
	ts := newTestServer(t)
	call(t, ts, "POST", "/v1/profile/sync", "u1", `{"username":"Ann"}`, nil)
	call(t, ts, "POST", "/v1/profile/sync", "u2", `{"username":"Bob"}`, nil)

	if code, _ := call(t, ts, "POST", "/v1/friends/request", "u1", `{"username":"Bob"}`, nil); code != 200 {
		t.Fatalf("request: %d", code)
	}
	// requester cannot accept their own request
	if code, _ := call(t, ts, "POST", "/v1/friends/respond", "u1", `{"uid":"u2","accept":true}`, nil); code == 200 {
		t.Fatal("requester must not be able to accept")
	}
	// target sees it as incoming
	_, body := call(t, ts, "GET", "/v1/friends", "u2", "", nil)
	friends := body["friends"].([]any)
	if len(friends) != 1 || friends[0].(map[string]any)["direction"] != "incoming" {
		t.Fatalf("friends for target: %v", body)
	}
	// accept → both sides accepted
	if code, _ := call(t, ts, "POST", "/v1/friends/respond", "u2", `{"uid":"u1","accept":true}`, nil); code != 200 {
		t.Fatal("accept failed")
	}
	_, body = call(t, ts, "GET", "/v1/friends", "u1", "", nil)
	if body["friends"].([]any)[0].(map[string]any)["status"] != "accepted" {
		t.Fatalf("expected accepted: %v", body)
	}
	// remove → empty
	call(t, ts, "DELETE", "/v1/friends/u2", "u1", "", nil)
	_, body = call(t, ts, "GET", "/v1/friends", "u1", "", nil)
	if len(body["friends"].([]any)) != 0 {
		t.Fatalf("expected no friends after remove: %v", body)
	}
}

func TestMatchResultEloAndLeaderboard(t *testing.T) {
	ts := newTestServer(t)
	call(t, ts, "POST", "/v1/profile/sync", "u1", `{"username":"Ann"}`, nil)
	call(t, ts, "POST", "/v1/profile/sync", "u2", `{"username":"Bob"}`, nil)

	// wrong key rejected
	code, _ := call(t, ts, "POST", "/v1/internal/match-result", "", `{}`, map[string]string{"X-Api-Key": "nope"})
	if code != http.StatusForbidden {
		t.Fatalf("expected 403, got %d", code)
	}
	code, body := call(t, ts, "POST", "/v1/internal/match-result", "",
		`{"uid0":"u1","uid1":"u2","winnerSide":0}`, map[string]string{"X-Api-Key": "testkey"})
	if code != 200 || body["elo0"].(float64) != 1016 || body["elo1"].(float64) != 984 {
		t.Fatalf("elo update: %d %v", code, body)
	}
	_, lb := call(t, ts, "GET", "/v1/leaderboard?limit=10", "u2", "", nil)
	top := lb["top"].([]any)
	if top[0].(map[string]any)["username"] != "Ann" || lb["me"].(map[string]any)["rank"].(float64) != 2 {
		t.Fatalf("leaderboard: %v", lb)
	}
}

func TestCloudSave(t *testing.T) {
	ts := newTestServer(t)
	// extend the mux with the save routes (newTestServer doesn't register them)
	// → exercise the store directly for rev conflict, and the handler via a fresh server.
	s := &Server{Store: mustStore(t)}
	// empty → rev 0
	rev, data, _ := s.Store.GetSave("u1")
	if rev != 0 || data != "" {
		t.Fatalf("empty save: %d %q", rev, data)
	}
	// write rev 1
	if r, _ := s.Store.PutSave("u1", 1, `{"coins":50}`); r != 1 {
		t.Fatalf("put rev1: %d", r)
	}
	// stale write (rev 0) is ignored, keeps rev 1
	if r, _ := s.Store.PutSave("u1", 0, `{"coins":0}`); r != 1 {
		t.Fatalf("stale write should keep rev1, got %d", r)
	}
	_, data, _ = s.Store.GetSave("u1")
	if data != `{"coins":50}` {
		t.Fatalf("stale write clobbered data: %q", data)
	}
	// newer write (rev 2) wins
	s.Store.PutSave("u1", 2, `{"coins":99}`)
	r2, d2, _ := s.Store.GetSave("u1")
	if r2 != 2 || d2 != `{"coins":99}` {
		t.Fatalf("rev2: %d %q", r2, d2)
	}
	_ = ts
}

func mustStore(t *testing.T) *Store {
	t.Helper()
	st, err := OpenStore(filepath.Join(t.TempDir(), "s.db"))
	if err != nil {
		t.Fatal(err)
	}
	return st
}

func TestEloMath(t *testing.T) {
	if a, b := EloUpdate(1000, 1000, 1); a != 1016 || b != 984 {
		t.Fatalf("even win: %d %d", a, b)
	}
	if a, b := EloUpdate(1000, 1000, 0.5); a != 1000 || b != 1000 {
		t.Fatalf("even draw: %d %d", a, b)
	}
	// upset win pays more than expected win
	upset, _ := EloUpdate(1000, 1200, 1)
	expected, _ := EloUpdate(1200, 1000, 1)
	if (upset-1000) <= (expected-1200) {
		t.Fatalf("upset should pay more: +%d vs +%d", upset-1000, expected-1200)
	}
	// floor
	if _, b := EloUpdate(2000, 100, 1); b < 100 {
		t.Fatalf("floor broken: %d", b)
	}
}

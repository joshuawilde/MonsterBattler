// MonsterBattler backend: profiles, Elo leaderboard, friends, push-device registry.
// Auth = Firebase ID tokens (verified via the Admin SDK); push = FCM. The battle server
// reports match results through an API-key internal endpoint and Elo is computed here.
package main

import (
	"log"
	"net/http"
	"os"
	"time"
)

func main() {
	addr := ":" + envOr("PORT", "8080")
	dbPath := envOr("DB_PATH", "monsterbattler.db")

	store, err := OpenStore(dbPath)
	if err != nil {
		log.Fatalf("store: %v", err)
	}

	auth, push := InitFirebase() // nil-safe in dev-bypass mode
	matchmaker := NewMatchmaker(store, NewBattleServers())

	s := &Server{Store: store, Auth: auth, Push: push, Match: matchmaker, InternalKey: os.Getenv("INTERNAL_API_KEY")}

	mux := http.NewServeMux()
	mux.HandleFunc("GET /healthz", func(w http.ResponseWriter, _ *http.Request) { w.Write([]byte("ok")) })
	mux.Handle("POST /v1/profile/sync", s.WithAuth(s.ProfileSync))
	mux.Handle("GET /v1/leaderboard", s.WithAuth(s.Leaderboard))
	mux.Handle("GET /v1/friends", s.WithAuth(s.FriendsList))
	mux.Handle("POST /v1/friends/request", s.WithAuth(s.FriendRequest))
	mux.Handle("POST /v1/friends/respond", s.WithAuth(s.FriendRespond))
	mux.Handle("DELETE /v1/friends/{uid}", s.WithAuth(s.FriendRemove))
	mux.Handle("POST /v1/devices", s.WithAuth(s.RegisterDevice))
	mux.Handle("POST /v1/match/queue", s.WithAuth(s.MatchQueue))
	mux.Handle("GET /v1/match/status", s.WithAuth(s.MatchStatus))
	mux.Handle("POST /v1/match/cancel", s.WithAuth(s.MatchCancel))
	mux.HandleFunc("POST /v1/internal/match-result", s.MatchResult) // X-Api-Key auth inside

	srv := &http.Server{
		Addr:              addr,
		Handler:           mux,
		ReadHeaderTimeout: 5 * time.Second,
	}
	log.Printf("listening on %s (db=%s, devBypass=%v)", addr, dbPath, devBypass())
	log.Fatal(srv.ListenAndServe())
}

func envOr(k, def string) string {
	if v := os.Getenv(k); v != "" {
		return v
	}
	return def
}

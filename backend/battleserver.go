package main

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"os"
	"strings"
	"time"
)

// BattleServers points matches at one (or more, round-robin) pure-dotnet WebSocket battle
// servers. On pair the backend registers the match (so the server only accepts known players),
// then hands clients the server's ws:// address. No per-match spawn — one long-running server
// hosts thousands of concurrent turn-based matches.
type BattleServers struct {
	httpBases []string // e.g. http://127.0.0.1:8081
	wsBases   []string // e.g. ws://127.0.0.1:8081/ws  (index-aligned with httpBases)
	key       string
	http      *http.Client
	rr        int
}

func NewBattleServers() *BattleServers {
	// BATTLE_SERVERS = comma-separated http base URLs for INTERNAL registration (compose network).
	// WS_PUBLIC_URLS = the public wss:// URL(s) handed to clients, index-aligned. When unset, the
	// public ws URL is derived from BATTLE_SERVERS (dev: direct ws://host:port/ws).
	raw := envOr("BATTLE_SERVERS", "http://127.0.0.1:8081")
	pub := os.Getenv("WS_PUBLIC_URLS")
	var httpBases, wsBases []string
	pubList := strings.Split(pub, ",")
	for i, b := range strings.Split(raw, ",") {
		b = strings.TrimRight(strings.TrimSpace(b), "/")
		if b == "" {
			continue
		}
		httpBases = append(httpBases, b)
		if pub != "" && i < len(pubList) && strings.TrimSpace(pubList[i]) != "" {
			wsBases = append(wsBases, strings.TrimSpace(pubList[i]))
		} else {
			ws := strings.Replace(strings.Replace(b, "http://", "ws://", 1), "https://", "wss://", 1)
			wsBases = append(wsBases, ws+"/ws")
		}
	}
	log.Printf("battle servers: register=%v public=%v", httpBases, wsBases)
	return &BattleServers{
		httpBases: httpBases, wsBases: wsBases,
		key:  os.Getenv("INTERNAL_API_KEY"),
		http: &http.Client{Timeout: 10 * time.Second},
	}
}

// Register a paired match on a battle server; returns the ws URL clients should connect to.
// bot=true → the battle server fills the opponent with an AI as soon as the player joins.
func (b *BattleServers) Register(ctx context.Context, matchID, uid0, uid1 string, bot bool) (string, error) {
	if len(b.httpBases) == 0 {
		return "", fmt.Errorf("no battle servers configured")
	}
	i := b.rr % len(b.httpBases)
	b.rr++
	body, _ := json.Marshal(map[string]any{"matchId": matchID, "uid0": uid0, "uid1": uid1, "bot": bot})
	req, err := http.NewRequestWithContext(ctx, "POST", b.httpBases[i]+"/internal/match", bytes.NewReader(body))
	if err != nil {
		return "", err
	}
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Api-Key", b.key)
	resp, err := b.http.Do(req)
	if err != nil {
		return "", err
	}
	defer resp.Body.Close()
	if resp.StatusCode >= 300 {
		msg, _ := io.ReadAll(io.LimitReader(resp.Body, 512))
		return "", fmt.Errorf("register %d: %s", resp.StatusCode, msg)
	}
	return b.wsBases[i], nil
}

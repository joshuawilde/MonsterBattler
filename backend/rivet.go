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
	"time"
)

// Rivet actor lifecycle. In stub mode (RIVET_TOKEN unset) Create returns a fixed dev endpoint
// (RIVET_STUB_HOST, default 127.0.0.1:7777) and Destroy is a no-op — so the whole matchmaking
// flow runs locally against the editor's host-mode server. In live mode it calls the Rivet API.
type Rivet struct {
	token   string
	project string
	env     string
	build   string // build tag name from rivet.json ("game")
	apiBase string
	backend string // BACKEND_URL passed into the actor's env
	intKey  string // INTERNAL_API_KEY passed into the actor's env
	http    *http.Client
}

type Actor struct {
	ID   string
	Host string
	Port int
}

func NewRivet() *Rivet {
	r := &Rivet{
		token:   os.Getenv("RIVET_TOKEN"),
		project: os.Getenv("RIVET_PROJECT"),
		env:     envOr("RIVET_ENV", "prod"),
		build:   envOr("RIVET_BUILD", "game"),
		apiBase: envOr("RIVET_API", "https://api.rivet.gg"),
		backend: os.Getenv("BACKEND_URL"),
		intKey:  os.Getenv("INTERNAL_API_KEY"),
		http:    &http.Client{Timeout: 30 * time.Second},
	}
	if r.token == "" {
		log.Printf("rivet: STUB mode (set RIVET_TOKEN + RIVET_PROJECT to go live); stub host=%s", r.stubHost())
	} else {
		log.Printf("rivet: live mode, project=%s env=%s build=%s", r.project, r.env, r.build)
	}
	return r
}

func (r *Rivet) Live() bool { return r.token != "" && r.project != "" }

func (r *Rivet) stubHost() string { return envOr("RIVET_STUB_HOST", "127.0.0.1:7777") }

// Create spawns a per-match actor and returns its public game endpoint.
func (r *Rivet) Create(ctx context.Context, matchID string) (Actor, error) {
	if !r.Live() {
		host, port := splitHostPort(r.stubHost())
		return Actor{ID: "stub-" + matchID, Host: host, Port: port}, nil
	}
	body := map[string]any{
		"tags":      map[string]string{"name": "battle-server", "match": matchID},
		"buildTags": map[string]string{"name": r.build, "current": "true"},
		"network": map[string]any{
			"ports": map[string]any{
				"game": map[string]any{"protocol": "udp", "internalPort": 7777, "routing": map[string]any{"host": map[string]any{}}},
			},
		},
		"resources": map[string]any{"cpu": 1000, "memory": 1024},
		"lifecycle": map[string]any{"durable": false},
		"runtime": map[string]any{
			"environment": map[string]string{"BACKEND_URL": r.backend, "INTERNAL_API_KEY": r.intKey},
		},
	}
	var out struct {
		Actor struct {
			ID      string `json:"id"`
			Network struct {
				Ports map[string]struct {
					Hostname string `json:"hostname"`
					Port     int    `json:"port"`
				} `json:"ports"`
			} `json:"network"`
		} `json:"actor"`
	}
	if err := r.call(ctx, "POST", "/actors", body, &out); err != nil {
		return Actor{}, err
	}
	g, ok := out.Actor.Network.Ports["game"]
	if !ok || g.Hostname == "" {
		return Actor{}, fmt.Errorf("rivet: actor %s has no game port yet", out.Actor.ID)
	}
	return Actor{ID: out.Actor.ID, Host: g.Hostname, Port: g.Port}, nil
}

func (r *Rivet) Destroy(ctx context.Context, actorID string) error {
	if !r.Live() || actorID == "" {
		return nil
	}
	return r.call(ctx, "DELETE", "/actors/"+actorID, nil, nil)
}

func (r *Rivet) call(ctx context.Context, method, path string, body, out any) error {
	var buf io.Reader
	if body != nil {
		b, _ := json.Marshal(body)
		buf = bytes.NewReader(b)
	}
	// Rivet scopes calls by project + environment query params.
	url := fmt.Sprintf("%s%s?project=%s&environment=%s", r.apiBase, path, r.project, r.env)
	req, err := http.NewRequestWithContext(ctx, method, url, buf)
	if err != nil {
		return err
	}
	req.Header.Set("Authorization", "Bearer "+r.token)
	req.Header.Set("Content-Type", "application/json")
	resp, err := r.http.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()
	if resp.StatusCode >= 300 {
		b, _ := io.ReadAll(io.LimitReader(resp.Body, 2048))
		return fmt.Errorf("rivet %s %s: %d %s", method, path, resp.StatusCode, b)
	}
	if out != nil {
		return json.NewDecoder(resp.Body).Decode(out)
	}
	return nil
}

func splitHostPort(hp string) (string, int) {
	host, port := "127.0.0.1", 7777
	for i := len(hp) - 1; i >= 0; i-- {
		if hp[i] == ':' {
			host = hp[:i]
			fmt.Sscanf(hp[i+1:], "%d", &port)
			break
		}
	}
	return host, port
}

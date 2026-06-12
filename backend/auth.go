package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"strings"

	firebase "firebase.google.com/go/v4"
	fbauth "firebase.google.com/go/v4/auth"
	"firebase.google.com/go/v4/messaging"
)

// InitFirebase returns the Auth + Messaging clients, or nils when running with
// AUTH_DEV_BYPASS=1 (local development without a Firebase project).
// Credentials come from GOOGLE_APPLICATION_CREDENTIALS (service-account JSON) or ADC.
func InitFirebase() (*fbauth.Client, *messaging.Client) {
	if devBypass() {
		log.Println("AUTH_DEV_BYPASS on — accepting 'Bearer dev:<uid>' tokens, push disabled")
		return nil, nil
	}
	app, err := firebase.NewApp(context.Background(), nil)
	if err != nil {
		log.Fatalf("firebase init: %v", err)
	}
	auth, err := app.Auth(context.Background())
	if err != nil {
		log.Fatalf("firebase auth: %v", err)
	}
	push, err := app.Messaging(context.Background())
	if err != nil {
		log.Fatalf("firebase messaging: %v", err)
	}
	return auth, push
}

func devBypass() bool { return os.Getenv("AUTH_DEV_BYPASS") == "1" }

type authedHandler func(w http.ResponseWriter, r *http.Request, uid string)

// WithAuth verifies the Firebase ID token in the Authorization header and passes the uid on.
// Dev bypass accepts "Bearer dev:<uid>" so every flow is locally testable.
func (s *Server) WithAuth(h authedHandler) http.Handler {
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		raw := strings.TrimPrefix(r.Header.Get("Authorization"), "Bearer ")
		if raw == "" || raw == r.Header.Get("Authorization") {
			http.Error(w, "missing bearer token", http.StatusUnauthorized)
			return
		}
		if devBypass() {
			if uid, ok := strings.CutPrefix(raw, "dev:"); ok && uid != "" {
				h(w, r, uid)
				return
			}
			http.Error(w, "dev token must be 'dev:<uid>'", http.StatusUnauthorized)
			return
		}
		tok, err := s.Auth.VerifyIDToken(r.Context(), raw)
		if err != nil {
			http.Error(w, "invalid token", http.StatusUnauthorized)
			return
		}
		h(w, r, tok.UID)
	})
}

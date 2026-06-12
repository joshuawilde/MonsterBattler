#!/bin/zsh
# Instant backend deploy from this machine — same steps as CI (test → image → push → restart),
# for when you don't want to wait for GitHub Actions.
#
#   DEPLOY_HOST=user@your-vps tools/deploy-backend.sh
#
# One-time: `docker login ghcr.io` here, and set the VPS up per backend/compose.yml header.
set -e
cd "$(dirname "$0")/../backend"

IMAGE=ghcr.io/joshuawilde/monsterbattler-backend:latest

echo "==> 1/4 tests"
go vet ./... && go test ./...

echo "==> 2/4 build image (linux/amd64)"
docker buildx build --platform linux/amd64 -t "$IMAGE" .

echo "==> 3/4 push"
docker push "$IMAGE"

if [ -z "$DEPLOY_HOST" ]; then
  echo "==> 4/4 skipped: set DEPLOY_HOST=user@host to restart the server"
  exit 0
fi
echo "==> 4/4 restart on $DEPLOY_HOST"
ssh "$DEPLOY_HOST" 'cd ~/monsterbattler && docker compose pull && docker compose up -d && docker image prune -f'
echo "deployed."

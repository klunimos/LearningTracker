#!/usr/bin/env bash
# One-shot deploy from your dev machine (run in Git Bash / WSL / any bash).
#
#   ./deploy.sh
#
# Does, in order:
#   1. Build the Angular client locally (the server has no Node).
#   2. git pull on the server (code must already be pushed to GitHub).
#   3. Apply DB schema (idempotent) — picks up any new scripts in deploy/init-db.sh.
#   4. Rebuild + restart the API container.
#   5. Upload the built frontend and swap it into ./www.
#   6. Verify the site + API respond.
#
# Options (env vars):
#   SKIP_FRONTEND=1   backend only (pull + schema + API), don't touch the client
#   SKIP_BACKEND=1    frontend only (build + upload), don't pull/build the API
#   DEPLOY_SERVER=ubuntu@51.38.113.223   override target
#   DEPLOY_DIR=/opt/LearningTracker      override remote path
set -euo pipefail

SERVER="${DEPLOY_SERVER:-ubuntu@51.38.113.223}"
REMOTE_DIR="${DEPLOY_DIR:-/opt/LearningTracker}"
CLIENT_DIR="learning-tracker-client"
BUILD_OUT="$CLIENT_DIR/dist/learning-tracker-client/browser"

cd "$(dirname "$0")"   # repo root

if [ "${SKIP_FRONTEND:-0}" != "1" ]; then
  echo "==> [1/5] Building frontend (production)..."
  ( cd "$CLIENT_DIR" && { [ -d node_modules ] || npm ci; } && npm run build )
  test -f "$BUILD_OUT/index.html" || { echo "!! build output missing at $BUILD_OUT"; exit 1; }
fi

if [ "${SKIP_BACKEND:-0}" != "1" ]; then
  echo "==> [2/5] Pulling latest code on server..."
  ssh "$SERVER" "cd '$REMOTE_DIR' && sudo git pull --ff-only"

  echo "==> [3/5] Applying DB schema (idempotent)..."
  ssh "$SERVER" "cd '$REMOTE_DIR' && sudo bash deploy/init-db.sh"

  echo "==> [4/5] Rebuilding + restarting the API..."
  ssh "$SERVER" "cd '$REMOTE_DIR' && sudo docker compose up -d --build api"
fi

if [ "${SKIP_FRONTEND:-0}" != "1" ]; then
  echo "==> [5/5] Uploading frontend + swapping into www..."
  ssh "$SERVER" "rm -rf /tmp/wwwnew && mkdir -p /tmp/wwwnew"
  scp -q -r "$BUILD_OUT"/* "$SERVER:/tmp/wwwnew/"
  ssh "$SERVER" "cd '$REMOTE_DIR' && test -f /tmp/wwwnew/index.html && sudo rm -rf www/* && sudo cp -a /tmp/wwwnew/. www/ && sudo rm -rf /tmp/wwwnew"
fi

echo "==> Verifying..."
ssh "$SERVER" "curl -s -o /dev/null -w 'chelkenu.org  HTTP %{http_code}\n' https://chelkenu.org; \
  curl -s -o /dev/null -w 'api (auth 401 expected)  HTTP %{http_code}\n' https://api.chelkenu.org/GroupGoal/GetMyParticipatingGoals; \
  cd '$REMOTE_DIR' && printf 'server commit: '; sudo git log --oneline -1"

echo "==> Done."

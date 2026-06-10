#!/usr/bin/env bash
# Creates the LearningTracker database and applies all schema scripts in order.
# Safe to re-run: every script guards its objects with IF NOT EXISTS.
set -euo pipefail

cd "$(dirname "$0")/.."        # repo root
set -a; . ./.env; set +a       # load SA_PASSWORD

SQLCMD=(docker exec -i lt-sqlserver /opt/mssql-tools18/bin/sqlcmd
        -S localhost -U sa -P "$SA_PASSWORD" -C)

echo "Waiting for SQL Server to accept connections..."
until "${SQLCMD[@]}" -Q "SELECT 1" </dev/null >/dev/null 2>&1; do
  sleep 2
done

echo "Ensuring database exists..."
"${SQLCMD[@]}" -Q "IF DB_ID('LearningTracker') IS NULL CREATE DATABASE [LearningTracker];" </dev/null

SCRIPTS=(
  "scripts/2026-02-26_d1-core-schema.sql"
  "scripts/2026-02-26_d2-groups-notifications-schema.sql"
  "scripts/2026-02-27_goal-isactive.sql"
  "scripts/2026-02-27_progress-entry-range.sql"
  "scripts/2026-03-15_group-profile-picture.sql"
  "LearningTracker.Api/Sql/CreateRefreshTokensTable.sql"
)

for f in "${SCRIPTS[@]}"; do
  echo "Applying $f ..."
  "${SQLCMD[@]}" -d LearningTracker < "$f"
done

echo "Database schema is up to date."

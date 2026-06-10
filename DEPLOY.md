# Deploy — Learning Tracker

Single-server deployment on an Ubuntu VPS using Docker Compose:
**Caddy** (auto-HTTPS) + **.NET 8 API** + **SQL Server Express**.

- `chelkenu.org` → Angular frontend (static files in `./www`)
- `api.chelkenu.org` → .NET API

Server: OVH VPS-1, Ubuntu, IP `51.38.113.223`.

---

## 0. DNS (do this first — certs need it)

At your domain registrar, create **A records** pointing to the server:

| Host | Type | Value |
|------|------|-------|
| `chelkenu.org` (`@`) | A | `51.38.113.223` |
| `www` | A | `51.38.113.223` |
| `api` | A | `51.38.113.223` |

Wait until they resolve (`nslookup api.chelkenu.org`) before step 4 — Let's Encrypt
validates over DNS+HTTP.

---

## 1. Connect + install Docker (run on the server, as root)

```bash
ssh root@51.38.113.223

# Docker engine + compose plugin
curl -fsSL https://get.docker.com | sh

# (optional) basic firewall
ufw allow 22/tcp && ufw allow 80/tcp && ufw allow 443/tcp && ufw --force enable
```

## 2. Get the code

```bash
cd /opt
git clone https://github.com/klunimos/LearningTracker.git
cd LearningTracker
```

## 3. Configure secrets

```bash
cp .env.example .env
nano .env          # set a strong SA_PASSWORD and JWT_KEY
# tip: openssl rand -base64 48   → use the output as JWT_KEY
```

## 4. Launch

```bash
docker compose up -d --build
```

This builds the API image and starts SQL Server, the API and Caddy. Caddy will
fetch HTTPS certificates automatically for the three hostnames.

## 5. Create the database schema (once)

```bash
bash deploy/init-db.sh
```

Creates the `LearningTracker` database and applies all schema scripts. Re-runnable.

## 6. Verify

```bash
curl -i https://api.chelkenu.org/Content/GetAll      # API responds (401/JSON)
docker compose ps                                    # all services "running"
docker compose logs -f api                            # watch API logs
```

---

## Frontend (Angular)

Build the client and copy its output into `./www` on the server.

**Important:** before building, point the Angular production environment at
`https://api.chelkenu.org` (in the client's `environment.prod.ts` / API base URL).

```bash
# in the learning-tracker-client repo (locally or on the server with Node installed)
npm ci
npm run build
# output: dist/learning-tracker-client/browser/

# copy the build into the server's www folder, e.g. from your machine:
scp -r dist/learning-tracker-client/browser/* root@51.38.113.223:/opt/LearningTracker/www/
```

Caddy serves it immediately at `https://chelkenu.org` (SPA fallback to `index.html`).

---

## Common operations

```bash
# Update after a new push
git pull && docker compose up -d --build

# Logs
docker compose logs -f api
docker compose logs -f caddy

# Restart everything
docker compose restart

# Backup the database volume (simple file copy of the data dir)
docker run --rm -v learningtracker_mssql-data:/data -v $(pwd):/backup alpine \
  tar czf /backup/mssql-backup-$(date +%F).tar.gz -C /data .
```

## Notes

- SQL Server is **not** exposed to the internet — only the API (inside the Docker
  network) can reach it. Only ports 80/443 are public (via Caddy).
- `MSSQL_PID=Express` → free edition, 10 GB DB limit (plenty here).
- The API runs behind Caddy; `ForwardedHeaders` is configured so HTTPS is honored.
- CORS allows `https://chelkenu.org` and `https://www.chelkenu.org`
  (set in `docker-compose.yml`).

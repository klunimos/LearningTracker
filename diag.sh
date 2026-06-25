#!/usr/bin/env bash
cd /opt/LearningTracker
set -a; . ./.env; set +a
echo "--- host 127.0.0.1:1433 TCP ---"
if timeout 3 bash -c "exec 3<>/dev/tcp/127.0.0.1/1433" 2>/dev/null; then echo OPEN; else echo REFUSED; fi
echo "--- SQL inside container ---"
docker exec lt-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -C -Q "SELECT COUNT(*) AS t FROM sys.tables" 2>&1 | head -6
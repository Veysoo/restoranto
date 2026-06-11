#!/bin/sh
set -e

echo "=== RestaurantOS baslatiliyor ==="

echo "SQL Server bekleniyor..."
retries=0
until nc -z sqlserver 1433 2>/dev/null; do
  retries=$((retries + 1))
  if [ "$retries" -ge 45 ]; then
    echo "SQL Server hazir degil, devam ediliyor..."
    break
  fi
  sleep 2
done

echo "API baslatiliyor (port 8081)..."
cd /app/api
dotnet RestaurantOS.Api.dll --urls http://0.0.0.0:8081 &
API_PID=$!

echo "API hazir bekleniyor..."
for i in $(seq 1 20); do
  if wget -q -O /dev/null http://127.0.0.1:8081/api/health 2>/dev/null; then
    echo "API hazir."
    break
  fi
  sleep 2
done

echo "Nginx baslatiliyor (port 80)..."
exec nginx -g 'daemon off;'

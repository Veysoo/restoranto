#!/bin/sh
set -e

echo "RestaurantOS API baslatiliyor..."
cd /app/api
dotnet RestaurantOS.Api.dll --urls http://0.0.0.0:8080 &

echo "Nginx baslatiliyor (port 80)..."
exec nginx -g 'daemon off;'

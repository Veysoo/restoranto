# ---- Build API ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS api-build
WORKDIR /src
COPY RestaurantOS.Domain/ RestaurantOS.Domain/
COPY RestaurantOS.Application/ RestaurantOS.Application/
COPY RestaurantOS.Infrastructure/ RestaurantOS.Infrastructure/
COPY RestaurantOS.Api/ RestaurantOS.Api/
RUN dotnet restore RestaurantOS.Api/RestaurantOS.Api.csproj
RUN dotnet publish RestaurantOS.Api/RestaurantOS.Api.csproj -c Release -o /app/api --no-restore

# ---- Build React ----
FROM node:20-alpine AS web-build
WORKDIR /web
COPY web/package.json web/package-lock.json* ./
RUN npm ci 2>/dev/null || npm install
COPY web/ ./
RUN npm run build

# ---- Runtime: API + Nginx (tek container) ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx \
    && rm -rf /var/lib/apt/lists/* \
    && rm -f /etc/nginx/sites-enabled/default

COPY --from=api-build /app/api /app/api
COPY --from=web-build /web/dist /usr/share/nginx/html
COPY docker/nginx.conf /etc/nginx/conf.d/restaurantos.conf
COPY docker/entrypoint.sh /entrypoint.sh
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 80

ENTRYPOINT ["/entrypoint.sh"]

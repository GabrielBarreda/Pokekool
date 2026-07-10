# syntax=docker/dockerfile:1
# ─────────────────────────────────────────────────────────────────────────────
# Image Web Pokekool = build Angular (Node) puis service statique via nginx.
# Contexte de build : ./src/web/poke-web
# ─────────────────────────────────────────────────────────────────────────────

# ---- Étape 1 : build Angular ----
FROM node:22-alpine AS build
WORKDIR /app

# Install déterministe (couche cachée tant que package*.json ne bouge pas)
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Build de production. Sortie du builder @angular/build:application : dist/pokekool-web/browser
RUN npm run build -- --configuration production

# ---- Étape 2 : nginx ----
FROM nginx:1.27-alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/pokekool-web/browser /usr/share/nginx/html
EXPOSE 80

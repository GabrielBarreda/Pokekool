# syntax=docker/dockerfile:1
# ─────────────────────────────────────────────────────────────────────────────
# Image API Pokekool = .NET 8 (ASP.NET runtime) + Blender headless.
#
# Contexte de build attendu : RACINE du repo (pour copier scripts/blender/).
#   docker build -f src/backend/Dockerfile -t pokekool-api .
# ou via docker-compose (context: . , dockerfile: src/backend/Dockerfile).
#
# Blender est requis par le module Stockage Physique (compilation .blend -> .glb).
# Playwright/Chromium N'EST PAS installé ici (init paresseuse, providers de scraping
# déjà partiellement désactivés) — à isoler dans un worker dédié si besoin plus tard.
# ─────────────────────────────────────────────────────────────────────────────

# ---- Étape 1 : build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore (couche cachée tant que les .csproj/props ne changent pas)
COPY src/backend/Directory.Build.props ./backend/Directory.Build.props
COPY src/backend/Pokekool.Domain/Pokekool.Domain.csproj                 ./backend/Pokekool.Domain/
COPY src/backend/Pokekool.Application/Pokekool.Application.csproj        ./backend/Pokekool.Application/
COPY src/backend/Pokekool.Infrastructure/Pokekool.Infrastructure.csproj ./backend/Pokekool.Infrastructure/
COPY src/backend/Pokekool.Shared/Pokekool.Shared.csproj                 ./backend/Pokekool.Shared/
COPY src/backend/Pokekool.API/Pokekool.API.csproj                       ./backend/Pokekool.API/
RUN dotnet restore ./backend/Pokekool.API/Pokekool.API.csproj

# Copie du reste des sources + publish
COPY src/backend/ ./backend/
RUN dotnet publish ./backend/Pokekool.API/Pokekool.API.csproj \
    -c Release -o /app/publish /p:UseAppHost=false

# ---- Étape 2 : runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Version de Blender (LTS). Modifiable au build : --build-arg BLENDER_VERSION=...
ARG BLENDER_VERSION=4.2.3
ARG BLENDER_MAJOR=4.2

# Dépendances runtime minimales pour Blender headless (--background, pas de display).
RUN apt-get update && apt-get install -y --no-install-recommends \
        wget xz-utils ca-certificates \
        libgl1 libxi6 libxxf86vm1 libxfixes3 libxrender1 libxkbcommon0 \
        libsm6 libice6 libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Téléchargement + installation de Blender dans /opt, lien dans le PATH.
RUN wget -q "https://download.blender.org/release/Blender${BLENDER_MAJOR}/blender-${BLENDER_VERSION}-linux-x64.tar.xz" \
        -O /tmp/blender.tar.xz \
    && mkdir -p /opt/blender \
    && tar -xf /tmp/blender.tar.xz -C /opt/blender --strip-components=1 \
    && ln -s /opt/blender/blender /usr/local/bin/blender \
    && rm /tmp/blender.tar.xz \
    && blender --background --version

# Application publiée
COPY --from=build /app/publish ./

# Script Blender companion (chemin attendu par appsettings : scripts/blender/export_glb.py)
COPY scripts/blender/ ./scripts/blender/

# Dossiers de travail du pipeline (montés en volume en prod pour la persistance)
RUN mkdir -p /app/wwwroot/models /app/App_Data/blender-uploads

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    Blender__ExecutablePath=blender

EXPOSE 8080
ENTRYPOINT ["dotnet", "Pokekool.API.dll"]

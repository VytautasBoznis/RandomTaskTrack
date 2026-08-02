# ── build stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Project files first so restore caches independently of source changes
COPY RandomTaskTrack.sln .
COPY RandomTaskTrack.API/RandomTaskTrack.API.csproj               RandomTaskTrack.API/
COPY RandomTaskTrack.Business/RandomTaskTrack.Business.csproj     RandomTaskTrack.Business/
COPY RandomTaskTrack.Data/RandomTaskTrack.Data.csproj             RandomTaskTrack.Data/
COPY RandomTaskTrack.Interfaces/RandomTaskTrack.Interfaces.csproj RandomTaskTrack.Interfaces/

RUN dotnet restore

COPY . .

RUN dotnet publish RandomTaskTrack.API/RandomTaskTrack.API.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

# ── runtime stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# tzdata so Scheduler:TimeZone resolves — without it every IANA id falls back
# to UTC and "today" rolls over at the wrong local hour.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "RandomTaskTrack.API.dll"]

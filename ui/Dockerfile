# ── build stage ──────────────────────────────────────────────────────────────
FROM node:22-alpine AS build
WORKDIR /src

# Manifests first so the install layer caches independently of source changes
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

RUN npm run build

# ── runtime stage ────────────────────────────────────────────────────────────
FROM nginx:1.29-alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /src/dist /usr/share/nginx/html

EXPOSE 8080

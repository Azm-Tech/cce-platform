# syntax=docker/dockerfile:1.7
# Build stage — install deps + nx production build
FROM node:22-alpine AS build
WORKDIR /workspace

RUN corepack enable

# Copy lockfile + package.json first so `pnpm install` is cached.
COPY frontend/package.json frontend/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

# Copy the rest of the workspace and build.
COPY frontend/ ./
RUN ./node_modules/.bin/nx build web-portal --configuration=production

# Runtime stage — nginx serving the SPA bundle
FROM nginx:alpine AS runtime
COPY frontend/apps/web-portal/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /workspace/dist/apps/web-portal/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -q -O /dev/null http://localhost:8080/ || exit 1

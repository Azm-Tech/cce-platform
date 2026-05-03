# syntax=docker/dockerfile:1.7
FROM node:22-alpine AS build
WORKDIR /workspace

RUN corepack enable

COPY frontend/package.json frontend/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile

COPY frontend/ ./
RUN ./node_modules/.bin/nx build admin-cms --configuration=production

FROM nginx:alpine AS runtime
COPY frontend/apps/admin-cms/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /workspace/dist/apps/admin-cms/browser /usr/share/nginx/html

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
  CMD wget -q -O /dev/null http://localhost:8080/ || exit 1

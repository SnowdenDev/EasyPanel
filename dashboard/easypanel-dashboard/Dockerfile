# Build context is this directory (dashboard/easypanel-dashboard) — the dashboard has no
# dependency on the rest of the monorepo, unlike the Backend/Daemon which share `contracts/`.

FROM node:26-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci

FROM node:26-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .
# No NEXT_PUBLIC_* backend URL needed — the browser never talks to the backend directly.
# EASYPANEL_BACKEND_URL (server-only, the internal Docker service address) is read at
# request time from a real environment variable, not baked in here.
RUN npm run build

FROM node:26-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
# The full node_modules from `npm ci`, not a traced subset — see next.config.ts for why:
# @microsoft/signalr's Node-side transport chain (ws, eventsource, tough-cookie, and
# further transitive deps under those) isn't reliably discoverable by Next's standalone
# output tracer, so this trades some image size for not silently missing one of them.
COPY --from=deps /app/node_modules ./node_modules
COPY --from=builder /app/public ./public
COPY --from=builder /app/.next ./.next
COPY --from=builder /app/package.json ./package.json

EXPOSE 3000
ENV PORT=3000
CMD ["npx", "next", "start"]

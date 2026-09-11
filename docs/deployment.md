# EasyPanel — Production Deployment Guide

## Status: what "production" actually means today

All three components are real, tested code as of 2026-09-10: Backend, Daemon,
and the Dashboard (`dashboard/easypanel-dashboard/`, Next.js + shadcn/ui).
The Dashboard was verified end-to-end twice — once locally, and again for
real: Backend/Dashboard/Postgres running via `docker compose` on a clean
Debian 13 VM, reverse-proxied to a real public HTTPS domain (via NetBird's
managed proxy, but the architecture doesn't care which reverse proxy you
use), with a real Daemon on a separate Windows machine launching a real
process — login, node registration, instance create/start/stop/restart,
live bidirectional console over the real public domain, staff account
creation. That pass also caught and fixed three real bugs that only showed
up under Docker/reverse-proxy conditions (a session cookie that silently
broke over plain HTTP, a path-validation check that assumed the Backend
runs on Windows, and a dependency-tracing gap in the Dashboard's Docker
image) — see architecture.md's Phase 3 gotchas for the details.

Known real gaps, not addressed by this guide, tracked in
[architecture.md](architecture.md#phase-3--dashboard-consumes-the-already-working-backend--daemon--done):
no file-directory browser (single-file download/upload by exact path only),
no live CPU/RAM in the console view, no way to list existing Staff accounts
in the UI (an account's ID is shown once at creation and must be pasted into
the permissions form), no instance-settings editing.

**Recommended path: Docker Compose** for the Backend + Dashboard + Postgres
(see below) — the Daemon is never containerized (see Topology for why) and
is always a native install on each Windows game-server host. A manual/
bare-metal walkthrough for the Backend/Dashboard follows further down for
anyone not using Docker.

## Topology

```
                    plain HTTP, behind YOUR reverse proxy
                    (nginx / Caddy / Traefik / cloud LB —
                     TLS termination is not this repo's job)
                                     │
                     ┌───────────────┴────────────────┐
                     ▼                                 ▼
              Dashboard (Next.js)  ────REST/SignalR──▶  Backend (ASP.NET Core)
                                                            │        ▲
              ── Docker Compose: dashboard + backend + postgres ──   │
                                                            │ Postgres│
                                                            ▼        │
                                                         Postgres 17
                                                            ▲
                                          outbound-only SignalR (2 connections)
                                                            │
                                         ┌──────────────────┴──────────────────┐
                                         │                                     │
                                   Daemon (node A)                      Daemon (node B)
                              runs the dedicated-server              runs the dedicated-server
                              executables on that host               executables on that host
                              — native Windows Service,              — native Windows Service,
                                NEVER containerized                    NEVER containerized
```

The Backend is the only component that needs an inbound public endpoint (the
Dashboard needs one too, for browsers, but never talks to the Daemon
directly). Daemons never accept inbound connections — they dial out. This
means a daemon node can sit behind NAT/a restrictive firewall with **zero
inbound firewall rules**; you only need outbound HTTPS (443, or whatever port
the backend listens on) allowed from the node.

**Why the Daemon is never in Docker, even though Backend/Dashboard/Postgres
are**: its entire job is supervising arbitrary Windows executables that
already exist on the *host* filesystem, using Windows Job Objects for
process-tree control (kill-on-job-close, CPU/RAM limits) — a Windows-kernel
feature a Linux container can't provide, and even Windows containers would
add a layer of process/filesystem indirection between the daemon and the
game-server executables it's supposed to directly own. It ships as a plain
self-contained `.exe` and runs as a Windows Service on the same machine as
the game servers, full stop.

## Quick start: Docker Compose (Backend + Dashboard + Postgres)

This is the recommended way to run the panel itself. It does **not** include
the Daemon — see [Configuring and running the Daemon](#configuring-and-running-the-daemon)
below for that; the Daemon is a separate install, once per Windows game-server
host, and is never part of this compose file.

```bash
cp .env.example .env
# edit .env: set POSTGRES_PASSWORD and JWT_SIGNING_KEY (openssl rand -base64 48)
docker compose up -d --build
```

That's it: three containers (`postgres`, `backend`, `dashboard`), one
`postgres-data` volume, and a shared network so `backend` and `dashboard` can
reach each other by service name (`http://backend:8080`) — the Dashboard
never needs to know the Backend's public address, because it's the only
thing that ever needs a public address at all. Migrations run automatically
on the backend container's first boot, same as the bare-metal path below.

**This publishes plain HTTP** — `dashboard` on `:3000` by default (override
via `DASHBOARD_PORT` in `.env`). Getting HTTPS in front of that is
explicitly **not this repo's responsibility** — put nginx, Caddy, Traefik,
NetBird, your cloud load balancer, whatever you already use, in front of
`dashboard`'s port, point your real domain at it, and let it handle
certificates. That's the *only* thing a browser ever needs to reach — see
Topology above for why the Backend itself has nothing browser-facing to
expose. `backend`'s `:5299` (`BACKEND_PORT`) only needs to be reachable by
the Daemon (see below) and, optionally, by you directly for debugging —
never by a browser, and never behind the same public hostname as the
Dashboard.

**First-boot admin password**: same as the bare-metal path — it's only ever
printed once, to the container's own log:

```bash
docker compose logs backend | grep "created the first Admin account"
```

**Rebuilding after a `.env` change**: `JWT_SIGNING_KEY`/`POSTGRES_PASSWORD`
are read by the backend at container *start*, so `docker compose up -d`
picks them up — no dashboard rebuild needed for anything in `.env` anymore,
since it no longer bakes in a backend URL at build time.

**Upgrading**: `git pull && docker compose up -d --build` — Postgres data
lives in the named volume, untouched by rebuilding the app images.

## Configuring and running the Daemon

The Daemon is not part of `docker compose` — install it directly on each
Windows machine that actually hosts game-server processes, once per machine.

1. **Register the node first**, from the Dashboard (Nodes → Register Node)
   or the API (`POST /api/nodes`, Admin JWT — see the bare-metal path's
   §4 below for the raw `curl`). You get back a `nodeId` and a
   `rawNodeToken` **shown exactly once** — the backend only ever stores
   its hash afterward. Write both down now.
2. **Publish the daemon** (needs the .NET 10 SDK, but only on the machine
   doing the publishing — the *target* Windows node needs nothing installed,
   the output is a self-contained native `.exe`):
   ```bash
   dotnet publish daemon/EasyPanel.Daemon/EasyPanel.Daemon.csproj -c Release -r win-x64
   ```
   Copy the whole `daemon/EasyPanel.Daemon/bin/Release/net10.0/win-x64/publish/`
   folder to the target node.
3. **Configure it** — either edit the copied `appsettings.json` directly, or
   (preferred, so the token isn't sitting in a plaintext file) override via
   environment variables on that machine, same double-underscore convention
   as the backend:
   ```
   NodeIdentity__NodeId=<nodeId from step 1>
   NodeIdentity__RawNodeToken=<rawNodeToken from step 1>
   NodeIdentity__BackendBaseUrl=https://panel-api.example.com
   NodeIdentity__DaemonVersion=<your release version string>
   NodeIdentity__HeartbeatIntervalSeconds=15
   ```
   `NodeIdentity__BackendBaseUrl` is wherever the Backend is reachable *from
   this node* — the Backend's own public address if the node is remote (put
   a reverse proxy in front of `BACKEND_PORT` the same way you did for the
   Dashboard, on its own hostname — don't reuse the Dashboard's hostname for
   this, they're deliberately separate concerns), or the LAN
   `host:BACKEND_PORT` address directly if the node is on the same network
   as the Backend host and you'd rather not expose it further. The daemon
   only ever calls *out* to this address; nothing needs to reach the daemon.
4. **Run it as a Windows Service** so it survives reboots and restarts on
   crash:
   ```powershell
   New-Service -Name "EasyPanelDaemon" `
     -BinaryPathName "C:\EasyPanel\daemon\EasyPanel.Daemon.exe" `
     -DisplayName "EasyPanel Daemon" `
     -StartupType Automatic
   sc.exe failure "EasyPanelDaemon" reset= 86400 actions= restart/5000
   Start-Service "EasyPanelDaemon"
   ```
   The daemon already retries its own connection to the backend with
   backoff if it's briefly unreachable — this service restart policy is
   only for the daemon process itself dying, a separate concern.
5. **Confirm it's alive**: `GET /api/nodes` (or the Dashboard's Nodes page,
   which updates live) should show `isOnline: true` shortly after the
   service starts.
6. **No inbound firewall rule needed on this machine at all** — the daemon
   only dials out. If outbound HTTPS to your backend's domain is blocked by
   a firewall/proxy on that network, that's the one thing to check.

Repeat steps 1–5 once per game-server host you're adding.

## Manual / bare-metal path (no Docker)

Everything below is the from-scratch walkthrough for running the Backend and
Dashboard directly on a host instead of via Docker Compose — useful for
understanding what the compose file automates, for a host where Docker
itself isn't an option, or for local development. If you followed the Docker
Compose quick start above, skip to
[Creating an instance](#5-creating-an-instance) once your daemon (above) is
running.

## 1. Prerequisites

- **Postgres 14+**, reachable from the Backend host. Needs a role that can
  `CREATE EXTENSION` on the target database (the first migration run issues
  `CREATE EXTENSION IF NOT EXISTS citext`) — use a superuser role for the
  first migration, or pre-create the extension yourself and use a
  lower-privilege role afterward.
- **Backend host**: Linux or Windows both work — the backend has no
  Windows-specific code (Job Objects and Native AOT are daemon-only). Needs
  either the ASP.NET Core 10 runtime installed, or publish it
  self-contained (see below) so nothing needs installing.
- **Daemon host(s)**: Windows only (Job Objects are a Windows API). The
  daemon publishes as a **self-contained Native AOT single executable** —
  the target Windows machine needs nothing installed, not even the .NET
  runtime.
- A reverse proxy in front of the Backend for TLS termination (nginx, Caddy,
  IIS, or a cloud load balancer) — Kestrel itself can serve HTTPS directly
  too, but a proxy in front is the simpler way to manage a real certificate
  and renewal.

## 2. Database setup

```bash
psql -h <postgres-host> -U postgres -c "CREATE DATABASE easypanel;"
psql -h <postgres-host> -U postgres -c "CREATE USER easypanel WITH PASSWORD '<strong-password>';"
psql -h <postgres-host> -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE easypanel TO easypanel;"
```

Migrations run automatically on Backend startup
(`dbContext.Database.MigrateAsync()` in `Program.cs`) — you do not run
`dotnet ef database update` separately in production. The first user to
connect and run migrations needs `CREATE EXTENSION` rights (see above); after
that first run, the app's normal role is enough.

## 3. Backend configuration

Never put real secrets in `appsettings.json` — it's committed to git.
`appsettings.json`'s `CHANGE_ME_...` placeholders are exactly that: for local
dev only, overridden with `dotnet user-secrets` (see
[CONTRIBUTING.md](../CONTRIBUTING.md)). In production, override the same two
keys via environment variables (ASP.NET Core's standard double-underscore
convention):

```bash
ConnectionStrings__Postgres="Host=<postgres-host>;Port=5432;Database=easypanel;Username=easypanel;Password=<strong-password>"
Jwt__SigningKey="<a long random secret, 32+ bytes, e.g. `openssl rand -base64 48`>"
Jwt__Issuer="EasyPanel.Backend"
Jwt__Audience="EasyPanel.Dashboard"
```

Generate the signing key once and keep it stable — rotating it invalidates
every issued JWT immediately (every logged-in session gets kicked out).

`ASPNETCORE_URLS` controls what Kestrel binds to, e.g.
`ASPNETCORE_URLS=http://0.0.0.0:5000` if a reverse proxy in front handles
TLS, or `https://0.0.0.0:5001` if Kestrel terminates TLS itself (needs a
certificate configured too — see the ASP.NET Core Kestrel HTTPS docs for
that, not covered here).

### Publish

```bash
dotnet publish backend/EasyPanel.Backend/EasyPanel.Backend.csproj -c Release -o ./publish/backend
```

Framework-dependent by default (needs the ASP.NET Core 10 runtime on the
host). Add `--self-contained true -r linux-x64` (or `win-x64`) if you'd
rather not install the runtime on the server.

### First run — capture the seeded admin password

On first startup against an empty `users` table, the Backend creates one
Admin account and logs the password **once**, as a warning-level log line
(`AdminAccountSeeder`). There is no other way to get it — if you lose that
log line before writing the password down, your only recovery path is
wiping the `users` table and letting it reseed.

```
warn: No users existed — created the first Admin account. Email: admin@localhost Password: <random-base64> (shown once, write it down now)
```

Run the first startup interactively (not silently backgrounded) so you can
actually see and capture that line, or make sure your log sink retains
warning-level logs from the very first boot.

## 4. Registering a node via the raw API

The [Configuring and running the Daemon](#configuring-and-running-the-daemon)
section above covers the full node-to-Windows-Service flow, using the
Dashboard to register the node. If you'd rather script it or don't have the
Dashboard up yet, registering a node is one authenticated call:

```bash
TOKEN=$(curl -s -X POST https://<backend-host>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{ "email": "admin@localhost", "password": "<...>" }' | jq -r .accessToken)

curl -X POST https://<backend-host>/api/nodes \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{ "displayName": "prod-node-1", "connectivityMode": "Remote" }'
```

Response: `{ "nodeId": "...", "rawNodeToken": "...", "connectivityMode": "Remote" }`.
**`rawNodeToken` is shown exactly once** — the Backend only ever stores its
SHA256 hash afterward and cannot show it again. There is no "regenerate
token" endpoint yet (a real MVP gap, see §7) — losing it means registering a
brand-new node. `connectivityMode: "Local"` lifts the file-manager size cap;
`"Remote"` caps file transfers at 10 MB — set this honestly based on whether
the node is actually on the same LAN as the Backend.

Take the returned `nodeId`/`rawNodeToken` to step 3 onward in the daemon
section above.

## 5. Creating an instance

```bash
curl -X POST https://<backend-host>/api/instances \
  -H "Authorization: Bearer <jwt>" \
  -H "Content-Type: application/json" \
  -d '{
    "nodeId": "<nodeId>",
    "displayName": "My Server",
    "workDirectory": "C:\\Servers\\myserver",
    "executableRelativePath": "server.exe",
    "expectedExecutableSha256": "<sha256 of server.exe>",
    "launchArguments": "--port 27015",
    "cpuLimitPercent": 50,
    "memoryLimitMegabytes": 2048
  }'
```

Get the hash either yourself (`certutil -hashfile server.exe SHA256` on
Windows, or `sha256sum`) or via the daemon round-trip:
`POST /api/nodes/{nodeId}/compute-hash` with the same relative path — the
Backend asks the daemon to hash the file on its own disk and returns the
result. Either way, **a mismatch at launch time is refused, not warned
about** — this is EasyPanel's core safety guarantee, don't work around it by
guessing a hash.

Then `POST /api/instances/{instanceId}/start` to launch it.

## 6. Deploying the Dashboard

The dashboard is a normal Next.js app — no daemon-style Native AOT/self-
contained story here, it needs a Node.js runtime on whatever host serves it.
Its Docker image (see the Dockerfile) copies the full `node_modules` rather
than relying on `next build`'s standalone output tracing — the server-side
SignalR connection it holds to the Backend (below) pulls in a chain of
optional Node dependencies (`ws`, `eventsource`, `tough-cookie`, ...) that
tracing doesn't reliably follow, so bare-metal installs should run a real
`npm install`/`npm ci` too rather than trying to hand-pick a minimal set.

```bash
cd dashboard/easypanel-dashboard
npm install
npm run build
```

Set this before `npm run build`/`npm start` (see `.env.example`):

```bash
EASYPANEL_BACKEND_URL=http://<backend-host>:<backend-port>
```

That's the only address this app needs to know. It's read **server-side
only** — by Server Actions/Route Handlers (every REST mutation, proxied with
the session's bearer token attached) and by the server-side SignalR
connection that backs the live console/node-status Server-Sent-Events
streams (`app/api/console-stream/...`, `app/api/nodes-stream`). The browser
never talks to the Backend directly for anything, so there's no browser-
facing backend URL to configure and no CORS policy to keep in sync with
it — see Topology above. This can be the Backend's plain internal/LAN
address; it doesn't need to be public or behind TLS itself, only reachable
from wherever the Dashboard process runs.

Run it with `npm start` behind your own reverse proxy/TLS termination, or on
whatever Node.js hosting you use (a systemd service running `npm start`, a
container, a platform like Vercel/Render — nothing about this app is tied to
a specific host). This is the *only* component in the stack a browser needs
to reach.

## 7. Known gaps before you rely on this in real production

These aren't bugs — they're MVP scope cuts already written down in
[architecture.md](architecture.md#decisions), repeated here specifically
because they matter for a production go-live decision:

- **No file-directory listing.** The dashboard's Files tab downloads/uploads
  one file at a time by exact path — there's no endpoint to browse a
  directory. Fine for config files and logs an operator already knows the
  path to; not a real file manager yet.
- **No live CPU/RAM in the console view.** Nothing on the wire carries it
  today (see architecture.md's Phase 3 gotchas).
- **No way to list existing accounts in the UI.** Staff account IDs are
  shown once at creation (no `GET /api/users`) — write them down, same as
  the node token.
- **No instance-settings editing.** Create/Start/Stop/Restart only; changing
  an existing instance's work directory, hash, or limits isn't wired up.
- **No node token rotation/regeneration endpoint.** Losing a node's raw
  token means re-registering that node from scratch.
- **No horizontal scaling.** One Backend process, one Postgres. SignalR
  group membership and the in-memory console ring buffer live in that one
  process's memory — running two Backend instances behind a load balancer
  will not work correctly without sticky sessions and a shared backplane,
  neither of which exist yet.
- **No backups.** Postgres backups (`pg_dump` on a cron, or your cloud
  provider's managed backup) are entirely your own responsibility. Console
  output history is an in-memory ring buffer only (~500 lines per
  instance) — it is not persisted anywhere and is lost on Backend restart.
- **No 2FA**, no scheduled tasks, no webhooks/notifications — all
  explicitly deferred (see architecture.md).
- **Offline nodes are rejected, not queued.** A command sent to an offline
  node returns an error immediately; there is no durable command queue.
- **TLS is entirely on you.** Kestrel/your reverse proxy needs a real
  certificate — nothing in this repo provisions one.

## 8. Pre-go-live checklist

- [ ] `Jwt__SigningKey` is a real random secret, not the placeholder, set via
      environment variable
- [ ] `ConnectionStrings__Postgres` uses a real password, set via environment
      variable, not committed anywhere
- [ ] Postgres is not reachable from the public internet (only from the
      Backend host / your VPC)
- [ ] TLS is terminated somewhere in front of the Backend (reverse proxy or
      Kestrel itself) — the daemon's `BackendBaseUrl` and every dashboard/API
      client use `https://`
- [ ] First-boot admin password was captured from the startup log and
      written down somewhere safe, then rotated to something the operator
      actually chose
- [ ] Each daemon's `appsettings.json`/environment carries the right
      `NodeId`/`RawNodeToken` pair and is not group-readable on disk
      (`icacls` it down on Windows)
- [ ] Each daemon runs as a Windows Service with an automatic-restart
      failure action
- [ ] Postgres backup job is actually scheduled and actually tested (a
      backup you've never restored from is not a backup)
- [ ] `connectivityMode` on every registered node matches reality (Local
      only if it's genuinely on the same LAN as the Backend — this directly
      controls the file-manager size cap)
- [ ] Dashboard's `EASYPANEL_BACKEND_URL` points at wherever the Backend is
      actually reachable from the Dashboard's own host/container (internal
      Docker network name, LAN address, or a private/public URL if they're
      on separate machines) — this is the only backend address the Dashboard
      needs, and it's never exposed to the browser
- [ ] Only the Dashboard's port is reverse-proxied under the public
      domain/TLS — the Backend's port is reachable only by the Daemon (and
      by you, for debugging), never behind the same public hostname as the
      Dashboard

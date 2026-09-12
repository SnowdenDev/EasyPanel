# EasyPanel

An open-source management panel for native Windows dedicated-server processes —
in the spirit of Pterodactyl / Pelican / AMP / MCSManager, but deliberately
different in one core way: **no Docker, no SteamCMD, no version management**.
EasyPanel launches and supervises executables that already exist on disk, and
its central safety guarantee is a SHA256 integrity check of the executable
before every launch.

Licensed under [AGPL-3.0](LICENSE).

[![CI](https://github.com/SnowdenDev/EasyPanel/actions/workflows/ci.yml/badge.svg)](https://github.com/SnowdenDev/EasyPanel/actions/workflows/ci.yml)
[![License: AGPL-3.0](https://img.shields.io/badge/License-AGPL--3.0-blue.svg)](LICENSE)

> **Project status:** active pre-1.0 development. Core authentication, node registration,
> instance lifecycle, live console, staff permissions, audit log, and exact-path file
> transfers are implemented. Fleet-history charts and the visual file browser still show
> clearly labelled preview data. Review the [deployment guide](docs/deployment.md) before
> exposing an installation to the internet.

## Architecture

Three components, one repo:

- **`backend/`** — ASP.NET Core (.NET 10), vertical-slice architecture, Postgres,
  JWT auth. The control plane: owns HTTPS, the public domain, users/roles, and
  all persisted state.
- **`daemon/`** — .NET 10, Native AOT, runs on each managed machine ("node").
  Connects **outbound only** to the backend over SignalR — nodes can sit behind
  NAT/firewalls with no inbound ports open. Launches processes wrapped in a
  Windows Job Object (no orphaned children, CPU/RAM limits, crash detection),
  streams live console output, and enforces the SHA256 launch gate.
- **`dashboard/`** — Next.js (TypeScript, SSR), UI built with shadcn/ui.

- **`contracts/`** — a plain C# class library referenced by both `backend` and
  `daemon`, holding the shared DTOs for their SignalR protocol. Changing a
  message shape is a compiler error on both ends, not a runtime surprise.

See [docs/architecture.md](docs/architecture.md) for the full design, including
the file-transfer relay design, the SignalR hub layout, and the MVP scope.

## Running it

**Backend + Dashboard + Postgres run in Docker** — that doesn't contradict
the "no Docker" rule above, which is about the game-server processes
EasyPanel manages, not the panel's own infrastructure.

```bash
cp .env.example .env
# edit .env: POSTGRES_PASSWORD, JWT_SIGNING_KEY, BACKEND_PUBLIC_URL, DASHBOARD_PUBLIC_ORIGIN
docker compose up -d --build
```

This publishes plain HTTP. Putting HTTPS in front (a real domain, a
certificate) is your reverse proxy's job, not this repo's — see
[docs/deployment.md](docs/deployment.md) for the full guide, including how
to get the one-time-only first admin password out of the logs.

**The Daemon is never in Docker.** It's a native Windows executable
(Native AOT, self-contained) that runs directly on each machine actually
hosting game-server processes — Windows Job Objects, which give it real
process-tree control (no orphaned children, CPU/RAM limits), are a Windows
kernel feature no container can substitute for. See
[docs/deployment.md](docs/deployment.md#configuring-and-running-the-daemon)
for how to register a node and install the daemon as a Windows Service.

For local development instead of Docker:

```bash
dotnet test EasyPanel.slnx -c Release
cd dashboard/easypanel-dashboard
npm ci
npm run lint
npm run build
```

See [docs/architecture.md](docs/architecture.md) for the phased build order
(Backend → Daemon → Dashboard) and [docs/deployment.md](docs/deployment.md)'s
"Manual / bare-metal path" for running each piece directly.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) first — it covers the vertical-slice
convention, validation commands, and pull-request workflow. Community changes arrive as
pull requests; protected-branch checks and code-owner review decide what reaches `main`.

Please report vulnerabilities privately according to [SECURITY.md](SECURITY.md) and follow
the [Code of Conduct](CODE_OF_CONDUCT.md) in all project spaces.

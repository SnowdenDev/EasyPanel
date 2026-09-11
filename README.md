# EasyPanel

An open-source management panel for native Windows dedicated-server processes —
in the spirit of Pterodactyl / Pelican / AMP / MCSManager, but deliberately
different in one core way: **no Docker, no SteamCMD, no version management**.
EasyPanel launches and supervises executables that already exist on disk, and
its central safety guarantee is a SHA256 integrity check of the executable
before every launch.

Licensed under [AGPL-3.0](LICENSE).

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
dotnet build EasyPanel.slnx
```

See [docs/architecture.md](docs/architecture.md) for the phased build order
(Backend → Daemon → Dashboard) and [docs/deployment.md](docs/deployment.md)'s
"Manual / bare-metal path" for running each piece directly.

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) first — it covers the vertical-slice
convention, naming standards, and why they matter for a project meant to stay
readable to outside contributors.

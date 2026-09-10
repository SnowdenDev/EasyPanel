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

## Getting started (development)

Prerequisites: .NET 10 SDK, Node.js + pnpm, a local Postgres instance (a
Docker Compose Postgres is fine for *your own dev machine* — that doesn't
contradict the "no Docker" rule, which is about the game-server processes
EasyPanel manages, not the panel's own dev tooling).

```bash
dotnet build EasyPanel.slnx
```

Backend, daemon, and dashboard run instructions will land here as each piece
comes online — see [docs/architecture.md](docs/architecture.md) for the phased
build order (Backend → Daemon → Dashboard).

## Contributing

Read [CONTRIBUTING.md](CONTRIBUTING.md) first — it covers the vertical-slice
convention, naming standards, and why they matter for a project meant to stay
readable to outside contributors.

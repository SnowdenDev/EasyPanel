# EasyPanel — Architecture

This is the living design document for EasyPanel. It started as an
implementation plan and gets updated as decisions change — treat it as the
source of truth, not a historical record.

## Why this exists

EasyPanel manages native Windows dedicated-server processes that already exist
on disk. Unlike Pterodactyl/Pelican/AMP, it does not use Docker and does not
manage versions or use SteamCMD — the admin points at a work directory and an
executable, and the daemon's core safety guarantee is verifying the
executable's SHA256 hash against an expected value before every launch,
refusing to launch on a mismatch.

The project is open source under AGPL-3.0. The architecture is chosen from day
one for maintainability and readability by outside contributors: vertical
slice in the backend, explicit human-readable naming, no premature
abstraction. See [CONTRIBUTING.md](../CONTRIBUTING.md) for the coding
standard itself.

## Decisions

- **Three components**: Backend (control plane), Dashboard (UI), Daemon (runs
  per node).
- **Backend**: C#, ASP.NET Core, vertical slice architecture, Postgres, JWT.
  Two roles — Admin and Staff — where Staff can be scoped to specific
  instances with granular per-action permission flags.
- **Dashboard**: TypeScript, Next.js, SSR, talks to backend via REST + SignalR.
  UI built with shadcn/ui (Tailwind + Radix primitives copied into the repo as
  source, not an opaque installed library — matches the readability standard).
- **Daemon**: C#, Native AOT, connects **outbound only** to the backend via
  SignalR — nodes can sit behind NAT/firewalls with no inbound ports open. The
  backend is the one component with a public HTTPS endpoint and domain.
- **No SteamCMD / no version management**: an instance is a work directory +
  executable path + expected SHA256 hash + launch arguments + environment
  variables. Nothing about the executable's contents or version is managed by
  EasyPanel beyond that hash check.
- **Windows Job Object** wraps every launched process: kill-on-job-close (no
  orphaned children if the daemon or process tree dies), CPU%/RAM limits, and
  the basis for crash detection.
- **Live console**: stdout/stderr streamed over SignalR to the dashboard;
  stdin can be sent back to the process from the dashboard.
- **File manager**: scoped strictly to an instance's work directory (path
  traversal guard is mandatory, not optional). A node flagged **Local/LAN** at
  registration gets no file-size limit; a **Remote** node is capped at 10 MB
  per file. Transfers use a **SignalR channel independent from the main
  control channel** so large transfers can't block console/command traffic,
  with token-bucket throttling.
- **Monorepo**: one repo containing `backend/`, `dashboard/`, `daemon/`,
  `contracts/` (a C# library shared between backend and daemon so protocol
  changes are compiler-checked on both ends).
- **License**: AGPL-3.0.
- **Target framework**: `net10.0` everywhere (.NET 10 is the current LTS as of
  this writing — .NET 9 was STS and is already out of support).

## Repository layout

```
EasyPanel/
├── EasyPanel.slnx
├── LICENSE                      # AGPL-3.0
├── README.md
├── CONTRIBUTING.md
├── docs/
│   ├── architecture.md          # this file
│   ├── signalr-protocol.md      # (to be written alongside the hubs)
│   └── adr/
├── contracts/EasyPanel.Contracts/           # net10.0, no ASP.NET/SignalR-client deps — plain DTOs
│   ├── Control/DaemonToBackend/             # DaemonHeartbeat, InstanceStatusChanged, ConsoleOutputLine, InstanceCrashed, InstanceStatusSnapshot, HashComputationResult
│   ├── Control/BackendToDaemon/             # LaunchInstanceCommand, StopInstanceCommand, SendConsoleInputCommand, ComputeExecutableHashCommand
│   ├── FileTransfer/                        # FileTransferRequest, FileChunk, FileTransferResult, FileTransferDirection
│   └── Enums/                               # InstanceStatus, NodeConnectivityMode, ConsoleStreamKind
├── backend/EasyPanel.Backend/                # net10.0, ASP.NET Core
│   ├── Features/                             # vertical slices — see CONTRIBUTING.md
│   ├── Infrastructure/                       # cross-cutting only: DbContext, hub shells, auth, audit log writer
│   └── Migrations/
├── backend/EasyPanel.Backend.Tests/
├── daemon/EasyPanel.Daemon/                  # net10.0, PublishAot, win-x64
│   ├── Features/                             # LaunchInstance, StopInstance, RestartPolicy, ConsoleStreaming, FileTransfer
│   ├── JobObjects/                           # JobObjectNativeMethods, JobObjectSafeHandle, ManagedJobObject
│   ├── Transport/                            # ControlHubConnection, FileTransferHubConnection
│   └── Infrastructure/                       # NodeIdentityOptions, HeartbeatService
├── daemon/EasyPanel.Daemon.Tests/
├── dashboard/easypanel-dashboard/             # Next.js + TS, pnpm, Tailwind + shadcn/ui
└── tools/generate-openapi-client.ps1
```

### Vertical slice example — `CreateInstance`

```
backend/EasyPanel.Backend/Features/Instances/CreateInstance/
├── CreateInstanceEndpoint.cs
├── CreateInstanceRequest.cs
├── CreateInstanceRequestValidator.cs
├── CreateInstanceHandler.cs
└── CreateInstanceResponse.cs
```

Design trap to avoid: the backend has **no direct filesystem access** to a
node's disk (nodes are outbound-only). The SHA256 hash is either declared by
the admin directly, or fetched from the daemon via a round trip
(`ComputeExecutableHash`) — the backend never reads a node's filesystem
itself.

No MediatR — a slice's endpoint calls its handler directly via DI. Fewer
dependencies, matches the "no premature abstraction" standard.

## Postgres schema (MVP)

UUID primary keys, `created_at_utc`/`updated_at_utc` on every table.

- **`users`** — email (citext, unique), password_hash (Argon2id via
  `Konscious.Security.Cryptography.Argon2`), display_name, `role`
  (`'Admin'`/`'Staff'` — a plain enum column; a full role table is
  over-engineering for two roles), is_active.
- **`staff_server_permissions`** — user_id, instance_id, and four boolean
  flags: `can_view_console`, `can_send_console_input`, `can_control_power`,
  `can_access_file_manager`, `can_edit_settings`. Unique on
  `(user_id, instance_id)`. Admin bypasses this table entirely in the
  authorization policy.
- **`nodes`** — display_name, `node_token_hash` (SHA256 of the node's auth
  token — the raw token is shown once at creation and never again),
  `connectivity_mode` (`'Local'`/`'Remote'` — this is what drives the 10 MB
  file-transfer cap), is_online, last_heartbeat_at_utc, daemon_version.
- **`instances`** — node_id, display_name, work_directory,
  executable_relative_path (relative to work_directory — enforces "the exe
  lives under the work dir" at the data model level), expected_executable_sha256,
  launch_arguments, environment_variables_json (jsonb), status,
  auto_restart_enabled, cpu_limit_percent, memory_limit_mb.
- **`port_allocations`** — instance_id, node_id, port, protocol (TCP/UDP),
  label. Unique on `(node_id, port, protocol)` — this is the actual
  conflict-prevention mechanism, enforced at the database level.
- **`audit_log_entries`** — actor_user_id (null = daemon/system), instance_id,
  node_id, action, details_json, ip_address. Indexed on
  `(instance_id, created_at_utc)` and `(actor_user_id, created_at_utc)`.

**Explicitly deferred** (not MVP): backups, scheduled tasks,
notifications/webhooks, 2FA, persisted console history (MVP keeps an in-memory
ring buffer of the last ~2000 lines per instance on the daemon), a
data-driven roles table, a durable offline-command queue (MVP rejects with a
clear error instead of queuing), historical metrics/time-series storage.

## SignalR design

Three separate hubs — kept separate because daemon connections and dashboard
connections have different trust levels and auth schemes; merging them risks a
bug letting a dashboard JWT reach daemon-only methods.

1. **`/hubs/daemon-control`** (`DaemonControlHub`) — the daemon's main control
   channel. Authenticated by **node token**, not user JWT — a custom
   `"NodeToken"` auth scheme hashes the presented token and compares it to
   `node_token_hash`. Each connection joins a SignalR group named after its
   `nodeId`; a reconnecting node forcibly aborts any existing connection for
   that `nodeId` first, to avoid stale duplicate connections.
2. **`/hubs/file-transfer`** (`FileTransferHub`) — a second, independent
   outbound connection from the daemon, dedicated to chunked file transfer.
   This isolation is what stops a large transfer from head-of-line-blocking
   console/command traffic on the control hub.
3. **`/hubs/dashboard`** (`DashboardHub`) — browser ↔ backend, standard JWT
   bearer auth, nothing bespoke needed here.

**Daemon → Backend methods**: `ReportHeartbeat`, `ReportInstanceStatusChanged`,
`ReportConsoleOutputLine`, `ReportInstanceCrashed`, `ComputeExecutableHash`,
`ReportCurrentInstanceStates` (sent once immediately after every reconnect, to
reconcile state after any connection gap).

**Backend → Daemon methods**: `LaunchInstance`, `StopInstance`,
`RestartInstance`, `KillInstance`, `SendConsoleInput`,
`ComputeExecutableHashRequest`.

**Daemon↔dashboard bridge**: when `DaemonControlHub.ReportConsoleOutputLine`
receives a line, the backend immediately re-broadcasts it to
`Clients.Group($"instance-console-{instanceId}")` on the `DashboardHub`. The
backend keeps a small in-memory ring buffer (~500 lines) per instance so a
dashboard client that subscribes late can backfill.

**Reconnect/backoff**: the daemon uses `WithAutomaticReconnect` with backoff
(0s, 2s, 5s, 10s, 30s), then a fixed 30s retry indefinitely if SignalR's
built-in reconnect gives up. While disconnected, the daemon keeps managing
already-running instances exactly as before — Job Objects don't depend on the
SignalR connection, and losing the connection to the panel is never a reason
to kill anything. On the backend side, if a target node is offline, commands
are **rejected immediately with a clear error** rather than queued — simpler
and more honest for the MVP than a durable command outbox (a natural v2
feature if this proves too blunt).

## File transfer

The daemon has no inbound port, so a browser can never reach it directly —
every transfer is relayed through the backend, which is the only component
with a public HTTPS endpoint. This is why the "independent channel" is itself
also an outbound SignalR connection from the daemon, rather than a direct
browser-to-daemon link: the relay is unavoidable regardless. The only real
design choice was whether that relay shares the control hub's connection
(rejected, head-of-line blocking risk) or is isolated (chosen).

Flow: browser calls `GET/PUT /api/instances/{id}/files/...` on the backend →
backend opens a `transferId` and asks the daemon (via `FileTransferHub`) to
start sending/receiving 64 KB chunks → backend bridges those chunks to the
open HTTP response stream (download) or request body (upload) via a
`Channel<byte[]>`. The path-traversal guard runs on the **daemon** (the actual
filesystem owner) — resolve to an absolute path, verify it stays under
`work_directory`, reject any symlink that escapes. The 10 MB cap for `Remote`
nodes is checked on the **backend** before a transfer even starts, since the
backend owns `connectivity_mode` — the daemon stays simple and doesn't need to
know about it. Throttling is a token bucket, recommended as one **shared
bucket per node** (not per transfer) so several simultaneous transfers can't
collectively saturate a node's uplink.

## Windows Job Object under Native AOT

Use `[LibraryImport]` (source-generated marshalling) instead of classic
`[DllImport]` for every Win32 call involved (`CreateJobObjectW`,
`AssignProcessToJobObject`, `SetInformationJobObject`, `TerminateJobObject`,
`CloseHandle`) — Native AOT removes the JIT-time marshalling stubs that
`DllImport`'s default marshalling relies on. The structs involved
(`JOBOBJECT_EXTENDED_LIMIT_INFORMATION`, `IO_COUNTERS`) are blittable
(primitive/`nint` fields only), so `LibraryImport` works without a custom
marshaller — the one thing to watch is any `BOOL` field, which needs an
explicit `[MarshalAs(UnmanagedType.Bool)]`. The job object handle is wrapped in
a `SafeHandle` subclass for deterministic cleanup, not a raw `IntPtr`/`nint`.

Required csproj settings: `<PublishAot>true</PublishAot>`,
`<RuntimeIdentifier>win-x64</RuntimeIdentifier>` (AOT requires a concrete RID),
`<SelfContained>true</SelfContained>`.

Limits set at job creation: `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` (the core
"no orphans" guarantee), `JOB_OBJECT_LIMIT_PROCESS_MEMORY` for the RAM cap,
`JOBOBJECT_CPU_RATE_CONTROL_INFORMATION` for the CPU% cap.

Crash/exit detection uses `Process.Exited`/`Process.ExitCode` on the already-
launched `Process` object (works fine under AOT, no reflection involved) — the
Job Object exists to guarantee no orphans and enforce resource limits, not as
the exit-detection mechanism itself.

## MVP build order — one phase per project

### Phase 1 — Backend (built and verified alone, before the daemon exists)

1. EF Core skeleton + migrations for `users`, `nodes`, `instances`,
   `port_allocations`, `staff_server_permissions`, `audit_log_entries`. JWT
   login. Admin seeded on first run.
2. `Features/Auth/Login`, `Features/Nodes/RegisterNode` (issues the node token
   once), `Features/Nodes/ListNodes`, `Features/Users/CreateUser` (Admin-only —
   a real gap in the original plan: without it there's no way to create the
   Staff accounts that `staff_server_permissions` exists to scope).
3. `Features/Instances/CreateInstance`, `StartInstance`, `StopInstance`,
   `GetInstanceStatus`.
4. `DaemonControlHub` (node-token auth), `DashboardHub` (JWT), `FileTransferHub`
   skeleton. Console bridge wired.
5. `Features/Staff/AssignServerPermissions` with the four flags enforced for
   real in authorization.
6. `Features/AuditLog/ListAuditEntries`.
7. **Verify without a real daemon**: a small SignalR test client connects to
   `DaemonControlHub` with a token from `RegisterNode`, simulates
   `ReportHeartbeat`/`ReportInstanceStatusChanged`, and confirms `nodes.is_online`,
   instance status, and the audit log all react correctly.

### Phase 2 — Daemon (built and verified against the already-working backend)

1. `Transport/ControlHubConnection` — outbound connection, token from local
   config, heartbeat, reconnect/backoff.
2. `JobObjects/` — `JobObjectNativeMethods`, `JobObjectSafeHandle`,
   `ManagedJobObject`.
3. `Features/LaunchInstance/ExecutableHashVerifier` +
   `LaunchedProcessRegistry` — SHA256 check, `Process.Start` wrapped in a Job
   Object, refusal on mismatch.
4. `Features/ConsoleStreaming` — `OutputPumpService`, `StdinForwarder`.
5. `Features/StopInstance`, `Features/RestartPolicy/CrashBackoffCalculator`.
6. `Features/FileTransfer` — second SignalR connection, `PathTraversalGuard`,
   `TokenBucketThrottle`.
7. **Verify**: run the real daemon against the Phase 1 backend, with a trivial
   test executable (prints an incrementing counter, echoes stdin). Covers hash
   mismatch, live console, kill-on-job-close, CPU/RAM limits, path traversal,
   the Local/Remote cap, and reconnect — all verifiable without the dashboard,
   using direct REST/hub calls.

### Phase 3 — Dashboard (consumes the already-working backend + daemon)

1. Next.js + Tailwind + shadcn/ui setup.
2. `lib/auth/getServerSession.ts` — JWT in an httpOnly cookie, SSR login.
3. `nodes` page — list + live online/offline status.
4. `instances` page — create instance, start/stop/restart.
5. `instances/[instanceId]/console` — live console + stdin input.
6. `instances/[instanceId]/files` — basic file manager respecting the
   Local/Remote cap.
7. `staff` page — assign per-server permissions.
8. `audit-log` page — simple list, no advanced filtering yet.
9. **Verify**: repeat the end-to-end checks below entirely through the UI.

**Deferred** (roadmap items, not MVP): backups, scheduled tasks,
notifications/webhooks, multi-node aggregate dashboard views, 2FA, a durable
offline-command queue, automated SignalR TypeScript codegen, historical
metrics.

## End-to-end verification checklist

Everything below runs on a single Windows dev machine (Postgres via Docker
Compose is fine for local dev tooling — that's not the same as the product's
"no Docker" rule, which is about the game-server processes EasyPanel manages).

1. Daemon connects with a valid token → `nodes.is_online` flips true in the DB
   and the dashboard reflects it live without a refresh.
2. Wrong hash → daemon refuses to launch, no new process appears, status goes
   to `HashMismatchRefused`, audit log records it. Fix the hash → it launches.
3. Test executable running → its counter appears in the dashboard console;
   text typed into the dashboard is echoed back by the process.
4. Force-kill the **daemon** process itself → the child process disappears
   immediately too (the actual proof of kill-on-job-close).
5. Low CPU%/RAM cap on a busy-looping/memory-allocating test executable →
   confirm the limit is enforced and that exceeding memory triggers the
   crash/auto-restart flow correctly.
6. Path traversal attempts (`../../../Windows/System32/...`, symlink escape,
   UNC tricks) against the file manager → rejected before touching the
   filesystem — needs a dedicated automated test, not just a manual check.
7. `Local` vs `Remote` node with an 11 MB file → `Remote` gets `413`, `Local`
   succeeds.
8. Kill the backend with an instance running → the daemon doesn't kill
   anything, retries reconnecting on the expected backoff, and reconciles
   cleanly once the backend comes back — no daemon restart needed.
9. A Staff user with only `can_view_console` → start/stop/files return `403`,
   console works, and every attempt (allowed or denied) writes an audit log
   entry.

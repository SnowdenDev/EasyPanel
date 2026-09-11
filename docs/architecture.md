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
`<SelfContained>true</SelfContained>`, and — despite only using blittable
structs — `<AllowUnsafeBlocks>true</AllowUnsafeBlocks>`: the LibraryImport
source generator itself emits unsafe code for `SetLastError`-marked P/Invokes
regardless of how clean the managed side is, and the build fails
(`SYSLIB1062`) without that flag.

Limits set at job creation: `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE` (the core
"no orphans" guarantee), `JOB_OBJECT_LIMIT_PROCESS_MEMORY` for the RAM cap,
`JOBOBJECT_CPU_RATE_CONTROL_INFORMATION` for the CPU% cap.

Crash/exit detection uses `Process.Exited`/`Process.ExitCode` on the already-
launched `Process` object (works fine under AOT, no reflection involved) — the
Job Object exists to guarantee no orphans and enforce resource limits, not as
the exit-detection mechanism itself.

## Gotchas hit building the daemon (Phase 2) — read before adding new wire messages

- **JSON under Native AOT**: `PublishAot=true` disables reflection-based
  `System.Text.Json` serialization — not only for a real `dotnet publish`, but
  for `dotnet build`/`dotnet run` too, so dev-time behavior matches what a
  published binary would actually do. Every DTO that crosses the daemon↔backend
  SignalR wire must be registered in
  `contracts/EasyPanel.Contracts/Serialization/ContractsJsonContext.cs`
  (`[JsonSerializable(typeof(...))]`), or sending/receiving it throws
  `Reflection-based serialization has been disabled...` at runtime — it still
  compiles fine, so this only surfaces when you actually exercise the new
  message. `ControlHubConnection` wires this context into the daemon's
  `HubConnectionBuilder` via `AddJsonProtocol`.
- **DI cycles through `IBackendReporter`**: don't add a constructor parameter
  of type `LaunchInstanceCommandHandler`/`StopInstanceCommandHandler` (or any
  other type that itself depends on `IBackendReporter`) to
  `ControlHubConnection`. `IBackendReporter` is registered to resolve back to
  `ControlHubConnection` itself — a direct constructor dependency on something
  that depends on `IBackendReporter` makes a real cycle
  (`ControlHubConnection` → X → `IBackendReporter` → `ControlHubConnection`),
  and the DI container **deadlocks instead of throwing**, because the cycle
  runs through an opaque factory delegate its call-site cycle detector can't
  see through — the process just hangs forever with no exception, no log line,
  and no network activity, which is a nasty thing to debug blind. Resolve such
  dependencies lazily via the injected `IServiceProvider` instead (see
  `ControlHubConnection`'s `LaunchInstanceCommandHandler`/
  `StopInstanceCommandHandler` properties).
- **SignalR's default 32 KB message size limit silently breaks file chunks**:
  `HubOptions.MaximumReceiveMessageSize` defaults to 32 KB. A 64 KB file chunk
  — base64-encoded plus JSON envelope overhead pushes it well past that —
  gets rejected, but SignalR does not surface this as a catchable exception on
  the sending side; it just closes the connection, so `SendAsync` calls for
  chunks made after the drop look like they succeeded (they're fire-and-forget
  and only check local connection state) while nothing ever arrives on the
  other end. A small control message (like the file-size metadata sent before
  the first chunk) stays under the limit and works fine, which makes the
  failure look chunk-specific and is a deceptive shape to debug. Fixed by
  raising `MaximumReceiveMessageSize` to 1 MB in `AddSignalR` on the backend —
  comfortably above anything a 64 KB chunk plus overhead can produce.
- **Node token hashing must hash the same bytes on both sides**: the raw token
  is generated as bytes and shown to the admin as a hex string
  (`Convert.ToHexString`), but the stored hash is `SHA256` of the **original
  bytes**, not of the UTF8 text of the hex string. `NodeTokenAuthenticationHandler`
  must `Convert.FromHexString` the presented token before hashing it, or every
  token fails to authenticate against its own freshly-issued hash.

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

### Phase 3 — Dashboard (consumes the already-working backend + daemon) — done

Built as `dashboard/easypanel-dashboard/` — Next.js App Router, TypeScript,
Tailwind, shadcn/ui (Radix primitives). No REST client library — Server
Components/Actions call the backend directly over plain `fetch` with the
bearer token attached server-side; the browser only ever holds it in memory
for the one thing that talks to the backend directly (the dashboard
SignalR connection), fetched fresh each time via a Server Action (see
`lib/actions/signalr-token.ts`).

1. Next.js + Tailwind + shadcn/ui setup — done.
2. `lib/session.ts` — the JWT (plus `displayName`, which isn't a JWT claim)
   lives in one httpOnly session cookie set by `lib/actions/auth.ts`'s
   `loginAction`; nothing server-rendered ever exposes it to client JS.
3. `nodes` page — list + live online/offline status, done for real: the
   backend already broadcasts `NodeConnectivityChanged` to every connected
   dashboard client (`DaemonControlHub.OnConnectedAsync`/`OnDisconnectedAsync`)
   — `components/dashboard/nodes-live-updater.tsx` just listens and calls
   `router.refresh()`. No polling needed.
4. `instances` page — create instance (with a real "compute hash from the
   node's own disk" round trip via the existing `ComputeExecutableHash`
   endpoint), start/stop/restart. Done.
5. `instances/[instanceId]/console` — live console + stdin input over the
   dashboard's own SignalR connection (`SubscribeToInstanceConsole`/
   `SendConsoleCommand`). Verified with a real daemon + the `EchoTestServer`
   fixture: live stdout, bidirectional stdin echo, and live status transitions
   all confirmed end-to-end, not just compiled.
6. `instances/[instanceId]/files` — **scoped down from the plan**: the
   backend has no directory-listing endpoint, only single-file
   download/upload by an exact relative path (see the gotcha below). Built a
   "download this path" / "upload to this path" panel against what actually
   exists rather than fabricating a browser it can't back.
7. `staff` page — create account + assign per-instance permissions. Done,
   with a real UX gap called out in the UI itself: there's no
   list-accounts endpoint either, so assigning permissions needs the
   account's ID pasted in (shown once right after creation).
8. `audit-log` page — simple list, Admin-only (matches the endpoint's own
   `RequireRole(Admin)`). Done.
9. **Verified for real**, not just built: see the Phase 3 section of the
   end-to-end checklist below — every item there was actually driven through
   the running UI against a real daemon process, not asserted from reading
   the code.

**Gotchas hit building the dashboard (Phase 3)**, in the same spirit as the
Phase 2 list above:

- **shadcn's Radix base + a Base UI-flavored preset don't mix silently.**
  `npx shadcn@latest init -b radix -p nova` generates components on Radix
  primitives, but the Nova preset's generated CSS uses Base UI's *boolean*
  state attributes (`data-open`, `data-closed`, `data-checked`, `data-active`,
  `data-horizontal`/`data-vertical`) as literal Tailwind custom variants —
  Radix primitives set `data-state="open"|"closed"|"checked"|"active"` and
  `data-orientation="horizontal"|"vertical"` instead, which those variants
  never match. The visible symptom isn't just "no animation": Radix's
  `Presence` unmounting logic waits for an animation/transition that a
  mismatched class set never actually starts, so `Dialog`/`Select`/
  `DropdownMenu` content **never unmounts once opened** — clicking outside,
  pressing Escape, and even the state going to `data-state="closed"` all
  "worked" internally while the DOM node stuck around forever. `Checkbox`'s
  checked style and `Tabs`' active-trigger style silently never applied for
  the same reason, with no unmount bug because those don't use `Presence`.
  Fixed by replacing every `data-open:`/`data-closed:`/`data-checked:`/
  `data-active:`/`data-horizontal:`/`data-vertical:` in
  `components/ui/{dialog,select,dropdown-menu,checkbox,tabs,separator}.tsx`
  with the bracket form (`data-[state=open]:`, `data-[orientation=horizontal]:`,
  etc.) that actually matches what Radix puts on the DOM, and dropped the
  open/close animation classes entirely on Dialog/Select/DropdownMenu rather
  than debug Presence's animation-detection further — an instant show/hide
  is a fine trade for "the dialog reliably closes." If a future contributor
  changes `-b`/`-p` on the shadcn CLI, or copies a new component from the
  registry with either flag, audit its generated classes for the same
  boolean-vs-bracket mismatch before trusting it.
- **No live CPU/RAM data reaches the backend at all.** The original mockup's
  console header showed live CPU%/RAM — there's no contract, no heartbeat
  field, nothing carrying that from the daemon to the backend today. Rather
  than fabricate numbers, the real dashboard's console header just omits
  them. Adding real resource metrics is a distinct piece of work: extend
  `DaemonHeartbeat` (or a new periodic message) with per-instance CPU/RAM
  read from the Job Object, and a new hub push to the dashboard.
- **The browser never talks to the backend directly — deliberately, not as a
  leftover.** An earlier version of this pass had the browser open the
  SignalR connection straight to the backend (with a matching CORS policy on
  `Program.cs`). The actual production requirement is narrower than that:
  only the Dashboard is meant to be public-facing; the Backend's own public
  exposure (if any) exists solely for the Daemon's outbound connection, a
  completely separate concern. So the live console/node-status feature was
  rebuilt to proxy through the Dashboard's own server instead: the real
  SignalR connection to `DashboardHub` lives server-side
  (`lib/signalr/server-connection.ts`, over the internal `EASYPANEL_BACKEND_URL`),
  and gets bridged to the browser as plain Server-Sent Events
  (`app/api/console-stream/[instanceId]` for output + status,
  `app/api/console-stream/[instanceId]/command` for sending input,
  `app/api/nodes-stream` for online/offline). The backend has no CORS policy
  at all now — nothing browser-originated ever reaches it.
- **`@microsoft/signalr` in Node.js drags in a real dependency chain, and
  Next's standalone output tracer doesn't reliably see all of it.** Running
  a `HubConnection` server-side (see above) needs `ws` (Node has no native
  WebSocket-as-a-package the way browsers do) and eagerly requires
  `eventsource` and `tough-cookie` too, regardless of which transport
  actually ends up used. Each one only surfaced as its own fresh
  `Cannot find module` at *request* time — after fixing the previous one —
  because Next's `output: "standalone"` file tracer doesn't follow dynamic
  `require()` calls made from inside a package already excluded from
  bundling (`serverExternalPackages`, needed separately because Turbopack's
  bundler can't handle `@microsoft/signalr`'s own internal dynamic require
  either — a distinct problem with the same package). Chasing each missing
  module one deploy at a time is not a sustainable way to find the rest of
  that chain. Fixed by dropping `output: "standalone"` for this app and
  copying the full `npm ci` `node_modules` into the runtime image instead
  (see the Dockerfile) — bigger image, but no more silently-missing
  transitive optional dependencies.
- **`Path.IsPathRooted` validates against the wrong machine's OS.**
  `CreateInstanceRequestValidator` used it to check that `WorkDirectory` is
  absolute — correct as long as the backend happened to run on Windows,
  which it always had until this pass moved it into a Linux container.
  `WorkDirectory` describes a path on the **daemon's** machine, which is
  always Windows (Job Objects), regardless of what OS the backend itself
  runs on. `Path.IsPathRooted("C:\\Servers\\x")` returns `false` on Linux,
  so every legitimate work directory an admin could enter got rejected as
  "not absolute" — caught by actually creating an instance against a real
  Dockerized backend, not by reading the code. The same OS-mismatch ran the
  other way for `ExecutableRelativePath` (checking *not* rooted): on Linux,
  an absolute Windows path also reads as "not rooted," so it would have
  silently passed a check meant to reject it. Fixed by matching the Windows
  path shape explicitly (a drive letter or a UNC prefix) instead of asking
  the runtime's own OS what "rooted" means.
- **Instances and nodes are now editable, deleting one still isn't.**
  `PUT /api/instances/{id}` and `PUT /api/nodes/{id}` exist (each a normal
  vertical slice — `UpdateInstance`/`UpdateNode` under their respective
  `Features/` folders) and the Dashboard's Nodes/Instances list rows are
  clickable through to a detail page with an edit form. There's still no
  delete for either — a stray test node or instance has to be removed
  directly in Postgres today. Editing an instance only takes effect the next
  time it's launched; it never touches a currently running process.

**Deferred** (roadmap items, not MVP): backups, scheduled tasks,
notifications/webhooks, multi-node aggregate dashboard views, 2FA, a durable
offline-command queue, automated SignalR TypeScript codegen, historical
metrics.

## End-to-end verification checklist

Everything below runs on a single Windows dev machine (Postgres via Docker
Compose is fine for local dev tooling — that's not the same as the product's
"no Docker" rule, which is about the game-server processes EasyPanel manages).

Items 1 and 3 were re-verified for real through the Phase 3 dashboard UI
(not just direct API/hub calls) on 2026-09-10: a node registered from the
UI showed online within ~1s of the daemon connecting with no manual refresh,
and a live `EchoTestServer` instance's console streamed real stdout and
echoed a command typed into the dashboard's own input, all over the
dashboard's SignalR connection.

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

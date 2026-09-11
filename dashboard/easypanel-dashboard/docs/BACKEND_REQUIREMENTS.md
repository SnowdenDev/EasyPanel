# Backend requirements — frontend-driven gaps

This file lists everything the dashboard's UI currently fakes because the .NET backend
(`GameVerse.Launcher.Core` / `GameVerse.Backend`) has no endpoint for it. Each item below
was mocked in the frontend so the UX could be designed and reviewed before the real data
exists. Search the frontend for `MockBadge` and `lib/mock-data.ts` to find every call site.

Give this file to whoever implements the backend side. Once an endpoint ships, replace the
matching mock call with a real `backendFetch` (see `src/lib/api.ts`) and delete the
`MockBadge` on that surface.

---

## 1. Fleet-wide telemetry history

**Used by:** Overview page (`/overview`) — "Fleet resource usage" chart.
**Currently:** `generateResourceHistory()` in `src/lib/mock-data.ts` fabricates 24 hourly
CPU/memory points client-side. Nothing is persisted; refreshing the page changes the shape
of the line.

**Needed endpoint:**

```
GET /api/telemetry/fleet-history?hours=24
```

Response:

```ts
{
  points: Array<{
    timestampUtc: string;   // ISO-8601, one per bucket
    avgCpuPercent: number;  // average across all online nodes at that bucket
    avgMemoryPercent: number;
  }>
}
```

Implementation notes:
- Requires the daemon heartbeat (`NodeSummary.cpuUsagePercent`, memory fields) to be sampled
  and stored on an interval (e.g. every 60s) rather than only exposing the latest snapshot.
  A simple `NodeMetricSample` table (`nodeId`, `capturedAtUtc`, `cpuPercent`, `memoryPercent`)
  written on every heartbeat, aggregated by hour, is enough for v1.
- `hours` should be clamped server-side (e.g. max 168 = 7 days) to bound the query.

---

## 2. Per-instance / per-node directory listing

**Used by:** Instance detail page → Files tab (`src/components/dashboard/files-panel.tsx`).
**Currently:** `generateMockFileTree(instanceId)` returns a hardcoded, plausible-looking
game-server folder structure (config/logs/world/etc). It is purely cosmetic — clicking a
file only pre-fills the existing real download path input, it does not read anything from
disk.

**Needed endpoint:**

```
GET /api/files/list?instanceId={id}&path={relativePath}
```

Response (one directory level, not recursive):

```ts
{
  path: string; // the requested relative path, normalized
  entries: Array<{
    name: string;
    type: "file" | "folder";
    sizeBytes: number | null; // null for folders
    modifiedAtUtc: string;
  }>;
}
```

Implementation notes:
- Reuse the same path-containment/sandboxing logic already used by the existing
  `/api/files/download` and `/api/files/upload` endpoints (must not escape the instance's
  work directory — reject `..` segments, symlink escapes, etc).
- Respects the same `Local` vs `Remote` node distinction already used for transfer size
  limits.
- Should honor `ServerPermissions.canAccessFileManager` the same way the existing file
  endpoints do.
- Once this exists, the frontend should replace the mock tree with real, lazily-loaded
  per-folder fetches (don't recursively list the whole tree up front).

---

## 3. (Already real, no change needed) Everything else on the Overview page

For clarity, these Overview page numbers are **not** mocked — they're computed from real
data already returned by existing endpoints, just aggregated client-side:

- Nodes online/total, offline node list → `GET /api/nodes`
- Instances running/total, crashed/hash-mismatch list → `GET /api/instances`
- Avg. CPU load → averaged from `NodeSummary.cpuUsagePercent` on `GET /api/nodes`
- Recent activity feed → `GET /api/audit-log?take=6` (Admin-only, matches existing
  `/audit-log` page behavior)

If a future iteration wants richer overview stats (e.g. "instances started today", "total
disk used"), those would need new aggregate endpoints too — not listed here since they
aren't implemented as mocks in the current UI, just absent.

/**
 * MOCKED DATA — everything in this file is generated client/server-side because the
 * .NET backend has no endpoint for it yet. Nothing here is persisted or real.
 *
 * See /docs/BACKEND_REQUIREMENTS.md for the exact endpoints another agent needs to build
 * so each of these can be swapped for a real `backendFetch` call.
 */

export interface ResourceHistoryPoint {
  time: string;
  cpu: number;
  memory: number;
}

/**
 * Deterministic-ish fake time series for the Overview resource chart.
 * Backend requirement: GET /api/telemetry/fleet-history?hours=24 aggregating node
 * heartbeats into buckets — see docs/BACKEND_REQUIREMENTS.md.
 */
export function generateResourceHistory(hours = 24): ResourceHistoryPoint[] {
  const points: ResourceHistoryPoint[] = [];
  let cpuSeed = 28;
  let memSeed = 44;

  for (let i = hours; i >= 0; i--) {
    const time = new Date(Date.now() - i * 60 * 60 * 1000);
    cpuSeed += (Math.sin(i * 0.7) + (Math.random() - 0.5)) * 4;
    memSeed += (Math.cos(i * 0.5) + (Math.random() - 0.5)) * 2.5;
    cpuSeed = Math.min(92, Math.max(8, cpuSeed));
    memSeed = Math.min(88, Math.max(20, memSeed));

    points.push({
      time: time.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
      cpu: Math.round(cpuSeed),
      memory: Math.round(memSeed),
    });
  }

  return points;
}

export interface MockFileEntry {
  name: string;
  type: "folder" | "file";
  sizeBytes: number | null;
  modifiedAtUtc: string;
  children?: MockFileEntry[];
}

/**
 * A believable-looking game server directory tree, used purely to preview what a real
 * directory browser should feel like. Backend requirement: GET
 * /api/files/list?instanceId=&path= returning entries for one directory level — see
 * docs/BACKEND_REQUIREMENTS.md.
 */
export function generateMockFileTree(seed: string): MockFileEntry {
  const hash = seed.length;
  return {
    name: "/",
    type: "folder",
    sizeBytes: null,
    modifiedAtUtc: new Date().toISOString(),
    children: [
      {
        name: "config",
        type: "folder",
        sizeBytes: null,
        modifiedAtUtc: new Date(Date.now() - 3600_000 * 4).toISOString(),
        children: [
          { name: "server.cfg", type: "file", sizeBytes: 2140, modifiedAtUtc: new Date(Date.now() - 3600_000 * 4).toISOString() },
          { name: "settings.json", type: "file", sizeBytes: 918, modifiedAtUtc: new Date(Date.now() - 3600_000 * 20).toISOString() },
          { name: "permissions.yml", type: "file", sizeBytes: 512, modifiedAtUtc: new Date(Date.now() - 3600_000 * 72).toISOString() },
        ],
      },
      {
        name: "logs",
        type: "folder",
        sizeBytes: null,
        modifiedAtUtc: new Date(Date.now() - 600_000).toISOString(),
        children: [
          { name: "latest.log", type: "file", sizeBytes: 48_302 + hash * 37, modifiedAtUtc: new Date(Date.now() - 600_000).toISOString() },
          { name: "crash-2024-01.log", type: "file", sizeBytes: 12_004, modifiedAtUtc: new Date(Date.now() - 3600_000 * 200).toISOString() },
        ],
      },
      {
        name: "world",
        type: "folder",
        sizeBytes: null,
        modifiedAtUtc: new Date(Date.now() - 3600_000 * 1).toISOString(),
        children: [
          { name: "region", type: "folder", sizeBytes: null, modifiedAtUtc: new Date(Date.now() - 3600_000 * 6).toISOString(), children: [] },
          { name: "level.dat", type: "file", sizeBytes: 4_112, modifiedAtUtc: new Date(Date.now() - 3600_000 * 1).toISOString() },
          { name: "level.dat_old", type: "file", sizeBytes: 4_098, modifiedAtUtc: new Date(Date.now() - 3600_000 * 25).toISOString() },
        ],
      },
      { name: "server.exe", type: "file", sizeBytes: 41_820_224, modifiedAtUtc: new Date(Date.now() - 3600_000 * 500).toISOString() },
      { name: "eula.txt", type: "file", sizeBytes: 148, modifiedAtUtc: new Date(Date.now() - 3600_000 * 500).toISOString() },
    ],
  };
}

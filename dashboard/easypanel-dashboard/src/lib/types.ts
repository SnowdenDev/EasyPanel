// Mirrors the backend's contracts and per-feature response DTOs. Changing a shape on the
// backend requires updating its mirror here in the same PR (see CONTRIBUTING.md) — there
// is no shared codegen between the C# backend and this app yet.

export type UserRole = "Admin" | "Staff";

export type NodeConnectivityMode = "Local" | "Remote";

export type InstanceStatus =
  | "Stopped"
  | "Starting"
  | "Running"
  | "Stopping"
  | "Crashed"
  | "HashMismatchRefused"
  | "Unknown";

export interface SessionUser {
  userId: string;
  email: string;
  displayName: string;
  role: UserRole;
}

export interface LoginResponse {
  accessToken: string;
  expiresAtUtc: string;
  userId: string;
  displayName: string;
  role: UserRole;
}

export interface NodeSummary {
  id: string;
  displayName: string;
  connectivityMode: NodeConnectivityMode;
  isOnline: boolean;
  lastHeartbeatAtUtc: string | null;
  daemonVersion: string | null;
  hostName: string | null;
  logicalProcessorCount: number | null;
  totalPhysicalMemoryMegabytes: number | null;
  availableMemoryMegabytes: number | null;
  cpuUsagePercent: number | null;
}

export interface InstanceSummary {
  id: string;
  displayName: string;
  nodeId: string;
  nodeDisplayName: string;
  status: InstanceStatus;
}

export interface InstanceStatusDetails {
  instanceId: string;
  displayName: string;
  status: InstanceStatus;
  nodeId: string;
  isNodeOnline: boolean;
  updatedAtUtc: string;
  workDirectory: string;
  executableRelativePath: string;
  expectedExecutableSha256: string;
  launchArguments: string | null;
  environmentVariables: Record<string, string> | null;
  cpuLimitPercent: number | null;
  memoryLimitMegabytes: number | null;
}

export interface AuditLogEntrySummary {
  id: string;
  actorUserId: string | null;
  instanceId: string | null;
  nodeId: string | null;
  action: string;
  detailsJson: string | null;
  createdAtUtc: string;
}

export interface UserSummary {
  id: string;
  email: string;
  displayName: string;
  role: UserRole;
  isActive: boolean;
  createdAtUtc: string;
}

export interface RegisterNodeResult {
  nodeId: string;
  rawNodeToken: string;
  connectivityMode: NodeConnectivityMode;
}

export interface ServerPermissions {
  canViewConsole: boolean;
  canSendConsoleInput: boolean;
  canControlPower: boolean;
  canAccessFileManager: boolean;
  canEditSettings: boolean;
}

export interface InstancePermissionSummary extends ServerPermissions {
  instanceId: string;
  instanceDisplayName: string;
}

export interface ApiError {
  message: string;
}

import type { InstanceStatus } from "@/lib/types";

const statusStyles: Record<InstanceStatus, string> = {
  Running: "bg-success/15 text-success",
  Starting: "bg-warning/15 text-warning",
  Stopping: "bg-warning/15 text-warning",
  Stopped: "bg-muted text-muted-foreground",
  Crashed: "bg-destructive/15 text-destructive",
  HashMismatchRefused: "bg-destructive/15 text-destructive",
  Unknown: "bg-muted text-muted-foreground",
};

const statusLabels: Record<InstanceStatus, string> = {
  Running: "Running",
  Starting: "Starting",
  Stopping: "Stopping",
  Stopped: "Stopped",
  Crashed: "Crashed",
  HashMismatchRefused: "Hash mismatch",
  Unknown: "Unknown",
};

export function StatusBadge({ status }: { status: InstanceStatus }) {
  return (
    <span
      className={`inline-flex items-center w-fit px-1.5 py-0.5 rounded text-[10px] font-semibold uppercase tracking-wide ${statusStyles[status]}`}
    >
      {statusLabels[status]}
    </span>
  );
}

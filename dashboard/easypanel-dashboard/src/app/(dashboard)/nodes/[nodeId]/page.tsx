import Link from "next/link";
import { notFound } from "next/navigation";
import { ChevronRight, Pencil } from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { NodeEditDialog } from "@/components/dashboard/node-edit-dialog";
import { StatusBadge } from "@/components/dashboard/status-badge";
import { Button } from "@/components/ui/button";
import type { InstanceSummary, NodeSummary } from "@/lib/types";

function timeAgo(iso: string | null): string {
  if (!iso) return "never";
  const seconds = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
  if (seconds < 60) return `${seconds}s ago`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  return `${Math.floor(seconds / 3600)}h ago`;
}

function formatMegabytes(megabytes: number | null): string {
  if (megabytes === null) return "—";
  return megabytes >= 1024 ? `${(megabytes / 1024).toFixed(1)} GB` : `${megabytes} MB`;
}

function StatCard({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1 rounded-lg border border-border px-4 py-3">
      <span className="text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">{label}</span>
      <span className="text-[13px]">{value}</span>
    </div>
  );
}

export default async function NodeDetailPage({ params }: { params: Promise<{ nodeId: string }> }) {
  const { nodeId } = await params;
  const session = await getServerSession();

  // No GET-by-id endpoint for a single node yet — the list is small enough that filtering
  // it here is simpler than standing up a second endpoint just for this page.
  const [nodes, instances] = await Promise.all([
    backendFetch<NodeSummary[]>("/api/nodes"),
    backendFetch<InstanceSummary[]>("/api/instances"),
  ]);
  const node = nodes.find((candidate) => candidate.id === nodeId);
  if (!node) {
    notFound();
  }

  const instancesOnThisNode = instances.filter((instance) => instance.nodeId === nodeId);

  return (
    <>
      <Topbar
        title={
          <div className="flex items-center gap-1.5">
            <Link href="/nodes" className="text-muted-foreground hover:text-foreground transition-colors">
              Nodes
            </Link>
            <ChevronRight size={13} className="text-muted-foreground" />
            <span>{node.displayName}</span>
          </div>
        }
        user={session!.user}
        allSystemsNormal={node.isOnline}
      />
      <div className="flex flex-col gap-6 p-8 flex-1 overflow-auto">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            <span className={`w-2 h-2 rounded-full shrink-0 ${node.isOnline ? "bg-success" : "bg-muted-foreground/50"}`} />
            <h1 className="font-mono text-[21px] font-semibold tracking-tight">{node.displayName}</h1>
          </div>
          <NodeEditDialog
            node={node}
            redirectOnDeleteTo="/nodes"
            trigger={<Button variant="secondary"><Pencil />Edit</Button>}
          />
        </div>

        <div className="flex flex-col gap-3">
          <h2 className="text-[14px] font-semibold">Connection</h2>
          <div className="grid grid-cols-4 gap-4">
            <StatCard label="Status" value={node.isOnline ? "Online" : "Offline"} />
            <StatCard label="Mode" value={node.connectivityMode} />
            <StatCard label="Daemon version" value={node.daemonVersion ?? "—"} />
            <StatCard label="Last heartbeat" value={timeAgo(node.lastHeartbeatAtUtc)} />
          </div>
        </div>

        <div className="flex flex-col gap-3">
          <h2 className="text-[14px] font-semibold">System</h2>
          <div className="grid grid-cols-4 gap-4">
            <StatCard label="Hostname" value={node.hostName ?? "—"} />
            <StatCard label="Logical CPUs" value={node.logicalProcessorCount?.toString() ?? "—"} />
            <StatCard
              label="CPU usage"
              value={node.cpuUsagePercent !== null ? `${node.cpuUsagePercent.toFixed(0)}%` : "—"}
            />
            <StatCard
              label="Memory"
              value={
                node.availableMemoryMegabytes !== null && node.totalPhysicalMemoryMegabytes !== null
                  ? `${formatMegabytes(node.totalPhysicalMemoryMegabytes - node.availableMemoryMegabytes)} / ${formatMegabytes(node.totalPhysicalMemoryMegabytes)}`
                  : "—"
              }
            />
          </div>
        </div>

        <div className="flex flex-col gap-3">
          <h2 className="text-[14px] font-semibold">Instances on this node</h2>
          {instancesOnThisNode.length === 0 ? (
            <p className="text-[13px] text-muted-foreground">No instances on this node yet.</p>
          ) : (
            <div className="rounded-lg border border-border overflow-hidden">
              {instancesOnThisNode.map((instance) => (
                <Link
                  key={instance.id}
                  href={`/instances/${instance.id}/console`}
                  className="flex items-center justify-between gap-4 px-4.5 min-h-10 py-2.5 border-b border-border last:border-b-0 hover:bg-muted/60 transition-colors"
                >
                  <span className="font-mono text-[13px]">{instance.displayName}</span>
                  <StatusBadge status={instance.status} />
                </Link>
              ))}
            </div>
          )}
        </div>

        <p className="text-xs text-muted-foreground font-mono">{node.id}</p>
      </div>
    </>
  );
}

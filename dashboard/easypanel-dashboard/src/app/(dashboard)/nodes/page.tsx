import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { RegisterNodeDialog } from "@/components/dashboard/register-node-dialog";
import { NodesLiveUpdater } from "@/components/dashboard/nodes-live-updater";
import type { NodeSummary } from "@/lib/types";

function timeAgo(iso: string | null): string {
  if (!iso) return "never";
  const seconds = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
  if (seconds < 60) return `${seconds}s ago`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  return `${Math.floor(seconds / 3600)}h ago`;
}

export default async function NodesPage() {
  const session = await getServerSession();
  const nodes = await backendFetch<NodeSummary[]>("/api/nodes");
  const onlineCount = nodes.filter((node) => node.isOnline).length;

  return (
    <>
      <Topbar title="Nodes" user={session!.user} allSystemsNormal={onlineCount === nodes.length} />
      <NodesLiveUpdater />
      <div className="flex flex-col gap-5 p-8 flex-1 overflow-auto">
        <div className="flex items-end justify-between">
          <div className="flex flex-col gap-1">
            <h1 className="text-[21px] font-semibold tracking-tight">Nodes</h1>
            <p className="text-[13px] text-muted-foreground">
              {onlineCount} of {nodes.length} online
            </p>
          </div>
          <RegisterNodeDialog />
        </div>

        <div className="rounded-lg border border-border overflow-hidden">
          <div className="flex items-center gap-4 px-4.5 py-2.5 bg-muted/60 border-b border-border text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">
            <div className="flex-1">Node</div>
            <div className="w-[120px]">Mode</div>
            <div className="w-[140px]">Daemon version</div>
            <div className="w-[120px]">Last heartbeat</div>
            <div className="w-6" />
          </div>

          {nodes.length === 0 ? (
            <div className="px-4.5 py-8 text-center text-[13px] text-muted-foreground">
              No nodes registered yet.
            </div>
          ) : (
            nodes.map((node) => (
              <Link
                key={node.id}
                href={`/nodes/${node.id}`}
                className="flex items-center gap-4 px-4.5 min-h-10 py-3 border-b border-border last:border-b-0 hover:bg-muted/60 transition-colors"
              >
                <div className="flex items-center gap-2.5 flex-1 min-w-0">
                  <span
                    className={`w-1.5 h-1.5 rounded-full shrink-0 ${node.isOnline ? "bg-success" : "bg-muted-foreground/50"}`}
                  />
                  <span className="font-mono text-[13px] font-medium truncate">{node.displayName}</span>
                </div>
                <div className="w-[120px] text-[12.8px] text-muted-foreground">{node.connectivityMode}</div>
                <div className="w-[140px] text-[12.8px] text-muted-foreground font-mono">
                  {node.daemonVersion ?? "—"}
                </div>
                <div className="w-[120px] text-[12.8px] text-muted-foreground">
                  {timeAgo(node.lastHeartbeatAtUtc)}
                </div>
                <ChevronRight size={15} className="w-6 text-muted-foreground shrink-0" />
              </Link>
            ))
          )}
        </div>
      </div>
    </>
  );
}

import Link from "next/link";
import { AlertTriangle, Boxes, ChevronRight, Cpu, Server, ShieldAlert } from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { PageHeader } from "@/components/dashboard/page-header";
import { StatCard } from "@/components/dashboard/stat-card";
import { EmptyState } from "@/components/dashboard/empty-state";
import { MockBadge } from "@/components/dashboard/mock-badge";
import { ResourceChart } from "@/components/dashboard/resource-chart";
import { RegisterNodeDialog } from "@/components/dashboard/register-node-dialog";
import { NewInstanceDialog } from "@/components/dashboard/new-instance-dialog";
import { generateResourceHistory } from "@/lib/mock-data";
import type { AuditLogEntrySummary, InstanceSummary, NodeSummary } from "@/lib/types";

function timeAgo(iso: string): string {
  const seconds = Math.max(0, Math.floor((Date.now() - new Date(iso).getTime()) / 1000));
  if (seconds < 60) return `${seconds}s ago`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ago`;
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h ago`;
  return `${Math.floor(seconds / 86400)}d ago`;
}

export default async function OverviewPage() {
  const session = await getServerSession();
  const isAdmin = session!.user.role === "Admin";

  const [nodes, instances, auditLog] = await Promise.all([
    backendFetch<NodeSummary[]>("/api/nodes"),
    backendFetch<InstanceSummary[]>("/api/instances"),
    isAdmin ? backendFetch<AuditLogEntrySummary[]>("/api/audit-log?take=6") : Promise.resolve<AuditLogEntrySummary[]>([]),
  ]);

  const onlineNodes = nodes.filter((node) => node.isOnline);
  const offlineNodes = nodes.filter((node) => !node.isOnline);
  const runningInstances = instances.filter((instance) => instance.status === "Running");
  const attentionInstances = instances.filter(
    (instance) => instance.status === "Crashed" || instance.status === "HashMismatchRefused",
  );
  const attentionCount = attentionInstances.length + offlineNodes.length;

  const cpuSamples = onlineNodes
    .map((node) => node.cpuUsagePercent)
    .filter((value): value is number => value !== null);
  const avgCpu = cpuSamples.length ? Math.round(cpuSamples.reduce((sum, v) => sum + v, 0) / cpuSamples.length) : null;

  const resourceHistory = generateResourceHistory();

  return (
    <>
      <Topbar title="Overview" user={session!.user} allSystemsNormal={attentionCount === 0} />
      <div className="flex-1 overflow-auto">
        <PageHeader
          title={`Welcome back, ${session!.user.displayName}`}
          description="A live snapshot of your fleet — nodes, instances, and what needs your attention."
        />

        <div className="flex flex-col gap-5 px-8 pb-10">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <StatCard
              icon={Server}
              label="Nodes online"
              value={`${onlineNodes.length}/${nodes.length}`}
              sub={offlineNodes.length ? `${offlineNodes.length} offline` : "All nodes reachable"}
              tone={offlineNodes.length ? "warning" : "success"}
            />
            <StatCard
              icon={Boxes}
              label="Instances running"
              value={`${runningInstances.length}/${instances.length}`}
              sub={`${instances.length - runningInstances.length} stopped, crashed, or starting`}
            />
            <StatCard
              icon={AlertTriangle}
              label="Needs attention"
              value={attentionCount}
              sub={attentionCount ? "Crashed instances or unreachable nodes" : "Nothing on fire"}
              tone={attentionCount ? "destructive" : "success"}
            />
            <StatCard
              icon={Cpu}
              label="Avg. CPU load"
              value={avgCpu !== null ? `${avgCpu}%` : "—"}
              sub="Current snapshot across online nodes"
            />
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
            <div className="lg:col-span-2 flex flex-col gap-4 p-5 rounded-lg border border-border bg-card">
              <div className="flex items-center justify-between">
                <div className="flex flex-col gap-0.5">
                  <h2 className="text-[14px] font-semibold text-foreground">Fleet resource usage</h2>
                  <p className="text-[12px] text-muted-foreground">Aggregate CPU and memory over the last 24 hours</p>
                </div>
                <MockBadge title="The backend has no telemetry-history endpoint yet — this chart is generated locally. See docs/BACKEND_REQUIREMENTS.md (GET /api/telemetry/fleet-history)." />
              </div>
              <ResourceChart data={resourceHistory} />
            </div>

            <div className="flex flex-col gap-3 p-5 rounded-lg border border-border bg-card">
              <h2 className="text-[14px] font-semibold text-foreground">Quick actions</h2>
              <p className="text-[12px] text-muted-foreground mb-1">Jump straight into the two most common tasks.</p>
              <div className="flex flex-col gap-2">
                <RegisterNodeDialog />
                <NewInstanceDialog nodes={nodes} />
              </div>
              <Link
                href="/nodes"
                className="flex items-center justify-between mt-2 px-3 py-2.5 rounded-md border border-border text-[12.5px] text-muted-foreground hover:bg-muted/60 hover:text-foreground transition-colors"
              >
                Manage nodes
                <ChevronRight size={14} />
              </Link>
              <Link
                href="/instances"
                className="flex items-center justify-between px-3 py-2.5 rounded-md border border-border text-[12.5px] text-muted-foreground hover:bg-muted/60 hover:text-foreground transition-colors"
              >
                Manage instances
                <ChevronRight size={14} />
              </Link>
            </div>
          </div>

          <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
            <div className="flex flex-col gap-1 rounded-lg border border-border bg-card overflow-hidden">
              <div className="flex items-center justify-between px-5 pt-4.5 pb-3">
                <h2 className="text-[14px] font-semibold text-foreground">Needs attention</h2>
                {attentionCount ? (
                  <span className="px-1.5 py-0.5 rounded-sm bg-destructive/15 text-destructive text-[11px] font-medium">
                    {attentionCount}
                  </span>
                ) : null}
              </div>
              {attentionCount === 0 ? (
                <EmptyState icon={ShieldAlert} title="All clear" description="Every node is reachable and every instance is healthy." />
              ) : (
                <div className="flex flex-col">
                  {offlineNodes.map((node) => (
                    <Link
                      key={node.id}
                      href={`/nodes/${node.id}`}
                      className="flex items-center gap-3 px-5 py-3 border-t border-border hover:bg-muted/60 transition-colors"
                    >
                      <span className="w-1.5 h-1.5 rounded-full bg-muted-foreground/50 shrink-0" />
                      <span className="flex-1 text-[13px] font-mono truncate">{node.displayName}</span>
                      <span className="text-[11.5px] text-muted-foreground">Node offline</span>
                      <ChevronRight size={14} className="text-muted-foreground shrink-0" />
                    </Link>
                  ))}
                  {attentionInstances.map((instance) => (
                    <Link
                      key={instance.id}
                      href={`/instances/${instance.id}/console`}
                      className="flex items-center gap-3 px-5 py-3 border-t border-border hover:bg-muted/60 transition-colors"
                    >
                      <span className="w-1.5 h-1.5 rounded-full bg-destructive shrink-0" />
                      <span className="flex-1 text-[13px] font-mono truncate">{instance.displayName}</span>
                      <span className="text-[11.5px] text-muted-foreground">
                        {instance.status === "HashMismatchRefused" ? "Hash mismatch" : "Crashed"}
                      </span>
                      <ChevronRight size={14} className="text-muted-foreground shrink-0" />
                    </Link>
                  ))}
                </div>
              )}
            </div>

            <div className="flex flex-col gap-1 rounded-lg border border-border bg-card overflow-hidden">
              <div className="flex items-center justify-between px-5 pt-4.5 pb-3">
                <h2 className="text-[14px] font-semibold text-foreground">Recent activity</h2>
                {isAdmin ? (
                  <Link href="/audit-log" className="text-[12px] text-primary hover:underline">
                    View all
                  </Link>
                ) : null}
              </div>
              {!isAdmin ? (
                <EmptyState
                  icon={ShieldAlert}
                  title="Admin only"
                  description="The audit log is restricted to Admin accounts."
                />
              ) : auditLog.length === 0 ? (
                <EmptyState icon={ShieldAlert} title="No activity yet" description="Actions across your fleet will show up here." />
              ) : (
                <div className="flex flex-col">
                  {auditLog.map((entry) => (
                    <div key={entry.id} className="flex items-center gap-3 px-5 py-3 border-t border-border">
                      <span className="flex-1 text-[13px] truncate">{entry.action}</span>
                      <span className="text-[11.5px] font-mono text-muted-foreground shrink-0">
                        {timeAgo(entry.createdAtUtc)}
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </>
  );
}

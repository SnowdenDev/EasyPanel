import { Boxes, CircleCheck, CircleX, Loader2 } from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { PageHeader } from "@/components/dashboard/page-header";
import { StatCard } from "@/components/dashboard/stat-card";
import { InstancesTable } from "@/components/dashboard/instances-table";
import { NewInstanceDialog } from "@/components/dashboard/new-instance-dialog";
import type { InstanceSummary, NodeSummary } from "@/lib/types";

export default async function InstancesPage() {
  const session = await getServerSession();
  const [instances, nodes] = await Promise.all([
    backendFetch<InstanceSummary[]>("/api/instances"),
    backendFetch<NodeSummary[]>("/api/nodes"),
  ]);

  const nodeCount = new Set(instances.map((instance) => instance.nodeId)).size;
  const running = instances.filter((instance) => instance.status === "Running").length;
  const transitioning = instances.filter((instance) => instance.status === "Starting" || instance.status === "Stopping").length;
  const attention = instances.filter(
    (instance) => instance.status === "Crashed" || instance.status === "HashMismatchRefused",
  ).length;

  return (
    <>
      <Topbar title="Instances" user={session!.user} allSystemsNormal={attention === 0} />
      <div className="flex-1 overflow-auto">
        <PageHeader
          title="Instances"
          description={`${instances.length} server${instances.length === 1 ? "" : "s"} across ${nodeCount} node${nodeCount === 1 ? "" : "s"}.`}
          actions={<NewInstanceDialog nodes={nodes} />}
        />

        <div className="flex flex-col gap-5 px-8 pb-10">
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
            <StatCard icon={Boxes} label="Total instances" value={instances.length} sub="Across all nodes" />
            <StatCard
              icon={CircleCheck}
              label="Running"
              value={running}
              tone="success"
              sub={`${Math.round(instances.length ? (running / instances.length) * 100 : 0)}% of fleet`}
            />
            <StatCard
              icon={Loader2}
              label="In transition"
              value={transitioning}
              tone={transitioning ? "warning" : "default"}
              sub="Starting or stopping"
            />
            <StatCard
              icon={CircleX}
              label="Needs attention"
              value={attention}
              tone={attention ? "destructive" : "success"}
              sub={attention ? "Crashed or hash mismatch" : "Nothing failing"}
            />
          </div>

          <InstancesTable instances={instances} />
        </div>
      </div>
    </>
  );
}

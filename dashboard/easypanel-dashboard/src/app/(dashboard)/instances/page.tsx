import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { InstancesTable } from "@/components/dashboard/instances-table";
import { NewInstanceDialog } from "@/components/dashboard/new-instance-dialog";
import type { InstanceSummary, NodeSummary } from "@/lib/types";

export default async function InstancesPage() {
  const session = await getServerSession();
  const [instances, nodes] = await Promise.all([
    backendFetch<InstanceSummary[]>("/api/instances"),
    backendFetch<NodeSummary[]>("/api/nodes"),
  ]);

  return (
    <>
      <Topbar title="Instances" user={session!.user} allSystemsNormal />
      <div className="flex flex-col gap-5 p-8 flex-1 overflow-auto">
        <div className="flex items-end justify-between">
          <div className="flex flex-col gap-1">
            <h1 className="text-[21px] font-semibold tracking-tight">Instances</h1>
            <p className="text-[13px] text-muted-foreground">
              {instances.length} server{instances.length === 1 ? "" : "s"} across{" "}
              {new Set(instances.map((instance) => instance.nodeId)).size} node
              {new Set(instances.map((instance) => instance.nodeId)).size === 1 ? "" : "s"}
            </p>
          </div>
          <NewInstanceDialog nodes={nodes} />
        </div>

        <InstancesTable instances={instances} />
      </div>
    </>
  );
}

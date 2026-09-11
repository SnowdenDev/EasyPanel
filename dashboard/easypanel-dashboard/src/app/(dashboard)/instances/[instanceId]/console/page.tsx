import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { ConsoleView } from "@/components/dashboard/console-view";
import { FilesPanel } from "@/components/dashboard/files-panel";
import { InstanceEditDialog } from "@/components/dashboard/instance-edit-dialog";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import type { InstanceStatusDetails } from "@/lib/types";

export default async function InstanceDetailPage({
  params,
}: {
  params: Promise<{ instanceId: string }>;
}) {
  const { instanceId } = await params;
  const session = await getServerSession();
  const instance = await backendFetch<InstanceStatusDetails>(`/api/instances/${instanceId}/status`);

  return (
    <>
      <Topbar
        title={
          <div className="flex items-center gap-1.5">
            <Link href="/instances" className="text-muted-foreground hover:text-foreground transition-colors">
              Instances
            </Link>
            <ChevronRight size={13} className="text-muted-foreground" />
            <span>{instance.displayName}</span>
          </div>
        }
        user={session!.user}
        allSystemsNormal={instance.isNodeOnline}
      />
      <div className="flex flex-col gap-4.5 p-7 flex-1 min-h-0">
        <div className="flex items-center justify-between">
          <div className="font-mono text-[18px] font-semibold tracking-tight">{instance.displayName}</div>
          <InstanceEditDialog instance={instance} redirectOnDeleteTo="/instances" />
        </div>

        <Tabs defaultValue="console" className="flex-1 min-h-0 flex flex-col gap-4.5">
          <TabsList>
            <TabsTrigger value="console">Console</TabsTrigger>
            <TabsTrigger value="files">Files</TabsTrigger>
          </TabsList>

          <TabsContent value="console" className="flex-1 min-h-0 flex flex-col">
            <ConsoleView instanceId={instanceId} initialStatus={instance.status} />
          </TabsContent>

          <TabsContent value="files">
            <FilesPanel instanceId={instanceId} />
          </TabsContent>
        </Tabs>
      </div>
    </>
  );
}

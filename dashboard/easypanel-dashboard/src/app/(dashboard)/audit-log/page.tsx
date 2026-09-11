import { redirect } from "next/navigation";
import { ScrollText } from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { PageHeader } from "@/components/dashboard/page-header";
import { EmptyState } from "@/components/dashboard/empty-state";
import type { AuditLogEntrySummary } from "@/lib/types";

export default async function AuditLogPage() {
  const session = await getServerSession();
  if (session!.user.role !== "Admin") {
    redirect("/instances");
  }

  const entries = await backendFetch<AuditLogEntrySummary[]>("/api/audit-log?take=100");

  return (
    <>
      <Topbar title="Audit Log" user={session!.user} allSystemsNormal />
      <div className="flex-1 overflow-auto">
        <PageHeader title="Audit Log" description={`Most recent ${entries.length} entries across your fleet.`} />

        <div className="flex flex-col gap-5 px-8 pb-10">
          <div className="rounded-lg border border-border overflow-hidden">
            <div className="flex items-center gap-4 px-4.5 py-2.5 bg-muted/60 border-b border-border text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">
              <div className="w-[150px]">When</div>
              <div className="flex-1">Action</div>
              <div className="w-[200px]">Actor</div>
            </div>

            {entries.length === 0 ? (
              <EmptyState icon={ScrollText} title="No entries yet" description="Actions across your fleet will show up here." />
            ) : (
              entries.map((entry) => (
              <div
                key={entry.id}
                className="flex items-center gap-4 px-4.5 py-3 border-b border-border last:border-b-0"
              >
                <div className="w-[150px] text-[12.3px] font-mono text-muted-foreground">
                  {new Date(entry.createdAtUtc).toLocaleString()}
                </div>
                <div className="flex-1 text-[13px] font-medium">{entry.action}</div>
                <div className="w-[200px] text-[12.3px] font-mono text-muted-foreground truncate">
                  {entry.actorUserId ?? "system / daemon"}
                </div>
              </div>
              ))
            )}
          </div>
        </div>
      </div>
    </>
  );
}

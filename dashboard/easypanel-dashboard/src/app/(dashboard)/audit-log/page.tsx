import { redirect } from "next/navigation";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
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
      <div className="flex flex-col gap-5 p-8 flex-1 overflow-auto">
        <div className="flex flex-col gap-1">
          <h1 className="text-[21px] font-semibold tracking-tight">Audit Log</h1>
          <p className="text-[13px] text-muted-foreground">Most recent {entries.length} entries.</p>
        </div>

        <div className="rounded-lg border border-border overflow-hidden">
          <div className="flex items-center gap-4 px-4.5 py-2.5 bg-muted/60 border-b border-border text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">
            <div className="w-[150px]">When</div>
            <div className="flex-1">Action</div>
            <div className="w-[200px]">Actor</div>
          </div>

          {entries.length === 0 ? (
            <div className="px-4.5 py-8 text-center text-[13px] text-muted-foreground">No entries yet.</div>
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
    </>
  );
}

import Link from "next/link";
import { notFound, redirect } from "next/navigation";
import { ChevronRight } from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { RoleBadge } from "@/components/dashboard/role-badge";
import { StaffPermissionMatrix } from "@/components/dashboard/staff-permission-matrix";
import type { InstancePermissionSummary, UserSummary } from "@/lib/types";

export default async function StaffMemberPage({ params }: { params: Promise<{ userId: string }> }) {
  const { userId } = await params;
  const session = await getServerSession();
  if (session!.user.role !== "Admin") {
    redirect("/instances");
  }

  // No GET-by-id endpoint for a single user yet — the list is small enough that filtering
  // it here is simpler than standing up a second endpoint just for this page.
  const users = await backendFetch<UserSummary[]>("/api/users");
  const member = users.find((candidate) => candidate.id === userId);
  if (!member) {
    notFound();
  }

  const permissions =
    member.role === "Staff"
      ? await backendFetch<InstancePermissionSummary[]>(`/api/staff/${userId}/permissions`)
      : [];

  return (
    <>
      <Topbar
        title={
          <div className="flex items-center gap-1.5">
            <Link href="/staff" className="text-muted-foreground hover:text-foreground transition-colors">
              Staff
            </Link>
            <ChevronRight size={13} className="text-muted-foreground" />
            <span>{member.displayName}</span>
          </div>
        }
        user={session!.user}
        allSystemsNormal
      />
      <div className="flex flex-col gap-6 p-8 flex-1 overflow-auto">
        <div className="flex items-center gap-3">
          <h1 className="text-[21px] font-semibold tracking-tight">{member.displayName}</h1>
          <RoleBadge role={member.role} />
        </div>

        <div className="grid grid-cols-3 gap-4 max-w-2xl">
          <div className="flex flex-col gap-1 rounded-lg border border-border px-4 py-3">
            <span className="text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">Email</span>
            <span className="text-[13px] truncate">{member.email}</span>
          </div>
          <div className="flex flex-col gap-1 rounded-lg border border-border px-4 py-3">
            <span className="text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">Status</span>
            <span className="text-[13px]">{member.isActive ? "Active" : "Disabled"}</span>
          </div>
          <div className="flex flex-col gap-1 rounded-lg border border-border px-4 py-3">
            <span className="text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">Created</span>
            <span className="text-[13px]">{new Date(member.createdAtUtc).toLocaleDateString()}</span>
          </div>
        </div>

        <div className="flex flex-col gap-3">
          <h2 className="text-[14px] font-semibold">Per-instance permissions</h2>
          {member.role === "Admin" ? (
            <p className="text-[13px] text-muted-foreground">
              Admin accounts already have full access to everything — per-instance grants only apply to Staff.
            </p>
          ) : (
            <StaffPermissionMatrix userId={userId} permissions={permissions} />
          )}
        </div>
      </div>
    </>
  );
}

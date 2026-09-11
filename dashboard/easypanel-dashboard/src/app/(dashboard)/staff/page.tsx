import { redirect } from "next/navigation";
import { ShieldCheck, Users } from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { PageHeader } from "@/components/dashboard/page-header";
import { StatCard } from "@/components/dashboard/stat-card";
import { CreateUserDialog } from "@/components/dashboard/create-user-dialog";
import { StaffUsersTable } from "@/components/dashboard/staff-users-table";
import type { UserSummary } from "@/lib/types";

export default async function StaffPage() {
  const session = await getServerSession();
  if (session!.user.role !== "Admin") {
    redirect("/instances");
  }

  const users = await backendFetch<UserSummary[]>("/api/users");
  const admins = users.filter((user) => user.role === "Admin").length;

  return (
    <>
      <Topbar title="Staff" user={session!.user} allSystemsNormal />
      <div className="flex-1 overflow-auto">
        <PageHeader
          title="Staff"
          description="Create accounts, then click one to scope what it can do, per instance."
          actions={<CreateUserDialog />}
        />

        <div className="flex flex-col gap-6 px-8 pb-10">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <StatCard icon={Users} label="Total accounts" value={users.length} sub="Admins and members" />
            <StatCard icon={ShieldCheck} label="Admins" value={admins} sub={`${users.length - admins} member${users.length - admins === 1 ? "" : "s"}`} />
          </div>

          <div className="flex flex-col gap-3">
            <h2 className="text-[14px] font-semibold">Accounts</h2>
            <StaffUsersTable users={users} />
          </div>
        </div>
      </div>
    </>
  );
}

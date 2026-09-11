import { redirect } from "next/navigation";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { CreateUserDialog } from "@/components/dashboard/create-user-dialog";
import { StaffUsersTable } from "@/components/dashboard/staff-users-table";
import type { UserSummary } from "@/lib/types";

export default async function StaffPage() {
  const session = await getServerSession();
  if (session!.user.role !== "Admin") {
    redirect("/instances");
  }

  const users = await backendFetch<UserSummary[]>("/api/users");

  return (
    <>
      <Topbar title="Staff" user={session!.user} allSystemsNormal />
      <div className="flex flex-col gap-8 p-8 flex-1 overflow-auto">
        <div className="flex items-end justify-between">
          <div className="flex flex-col gap-1">
            <h1 className="text-[21px] font-semibold tracking-tight">Staff</h1>
            <p className="text-[13px] text-muted-foreground">
              Create accounts, then click one to scope what it can do, per instance.
            </p>
          </div>
          <CreateUserDialog />
        </div>

        <div className="flex flex-col gap-3">
          <h2 className="text-[14px] font-semibold">Accounts</h2>
          <StaffUsersTable users={users} />
        </div>
      </div>
    </>
  );
}

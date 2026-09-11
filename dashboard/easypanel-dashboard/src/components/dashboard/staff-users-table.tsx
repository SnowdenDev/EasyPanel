import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { RoleBadge } from "@/components/dashboard/role-badge";
import type { UserSummary } from "@/lib/types";

export function StaffUsersTable({ users }: { users: UserSummary[] }) {
  return (
    <div className="rounded-lg border border-border overflow-hidden">
      <div className="flex items-center gap-4 px-4.5 py-2.5 bg-muted/60 border-b border-border text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">
        <div className="flex-1">User</div>
        <div className="w-[90px]">Role</div>
        <div className="w-[110px]">Status</div>
        <div className="w-6" />
      </div>

      {users.length === 0 ? (
        <div className="px-4.5 py-8 text-center text-[13px] text-muted-foreground">No accounts yet.</div>
      ) : (
        users.map((user) => (
          <Link
            key={user.id}
            href={`/staff/${user.id}`}
            className="flex items-center gap-4 px-4.5 min-h-10 py-3 border-b border-border last:border-b-0 hover:bg-muted/60 transition-colors"
          >
            <div className="flex flex-col gap-0.5 flex-1 min-w-0">
              <span className="text-[13px] font-medium truncate">{user.displayName}</span>
              <span className="text-[12px] text-muted-foreground truncate">{user.email}</span>
            </div>
            <div className="w-[90px]">
              <RoleBadge role={user.role} />
            </div>
            <div className="w-[110px] text-[12.8px] text-muted-foreground">
              {user.isActive ? "Active" : "Disabled"}
            </div>
            <ChevronRight size={15} className="w-6 text-muted-foreground shrink-0" />
          </Link>
        ))
      )}
    </div>
  );
}

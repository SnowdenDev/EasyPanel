import type { UserRole } from "@/lib/types";

const roleStyles: Record<UserRole, string> = {
  Admin: "bg-primary/15 text-primary",
  Staff: "bg-muted text-muted-foreground",
};

export function RoleBadge({ role }: { role: UserRole }) {
  return (
    <span
      className={`inline-flex items-center w-fit px-1.5 py-0.5 rounded text-[10px] font-semibold uppercase tracking-wide ${roleStyles[role]}`}
    >
      {role}
    </span>
  );
}

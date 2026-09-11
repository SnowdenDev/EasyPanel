import { LogOut } from "lucide-react";
import { logoutAction } from "@/lib/actions/auth";
import { ThemeToggle } from "@/components/dashboard/theme-toggle";
import { AccentPicker } from "@/components/dashboard/accent-picker";
import type { SessionUser } from "@/lib/types";

export function Topbar({
  title,
  user,
  allSystemsNormal,
}: {
  title: React.ReactNode;
  user: SessionUser;
  allSystemsNormal: boolean;
}) {
  return (
    <div className="flex items-center justify-between h-[54px] px-6 border-b border-border shrink-0">
      <div className="text-[13.5px] font-semibold text-foreground">{title}</div>
      <div className="flex items-center gap-3.5">
        <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-md border border-border">
          <span
            className={`w-1.5 h-1.5 rounded-full shrink-0 ${allSystemsNormal ? "bg-success" : "bg-destructive"}`}
          />
          <span className="text-xs text-muted-foreground">
            {allSystemsNormal ? "All systems normal" : "Attention needed"}
          </span>
        </div>
        <div className="flex items-center gap-0.5">
          <AccentPicker />
          <ThemeToggle />
        </div>
        <div className="w-px h-4.5 bg-border" />
        <div
          title={user.email}
          className="w-7 h-7 rounded-full bg-primary/25 flex items-center justify-center text-[11.5px] font-semibold text-primary shrink-0"
        >
          {user.displayName.slice(0, 1).toUpperCase()}
        </div>
        <form action={logoutAction}>
          <button
            type="submit"
            title="Sign out"
            className="flex items-center justify-center w-7 h-7 rounded-md text-muted-foreground hover:bg-muted hover:text-foreground transition-colors"
          >
            <LogOut size={15} />
          </button>
        </form>
      </div>
    </div>
  );
}

import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/utils";

const toneClasses = {
  default: "text-foreground",
  success: "text-success",
  warning: "text-warning",
  destructive: "text-destructive",
} as const;

export function StatCard({
  icon: Icon,
  label,
  value,
  sub,
  tone = "default",
  mockBadge,
}: {
  icon: LucideIcon;
  label: string;
  value: React.ReactNode;
  sub?: React.ReactNode;
  tone?: keyof typeof toneClasses;
  mockBadge?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col gap-3 p-4.5 rounded-lg border border-border bg-card">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2 text-muted-foreground">
          <Icon size={14.5} />
          <span className="text-[12px] font-medium uppercase tracking-wide">{label}</span>
        </div>
        {mockBadge}
      </div>
      <div className="flex items-baseline gap-2">
        <span className={cn("text-[26px] font-semibold tracking-tight tabular-nums", toneClasses[tone])}>{value}</span>
      </div>
      {sub ? <p className="text-[12px] text-muted-foreground">{sub}</p> : null}
    </div>
  );
}

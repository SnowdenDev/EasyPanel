import type { LucideIcon } from "lucide-react";

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
}: {
  icon: LucideIcon;
  title: string;
  description?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-14 px-6 text-center">
      <div className="flex items-center justify-center w-11 h-11 rounded-full bg-muted text-muted-foreground">
        <Icon size={19} />
      </div>
      <div className="flex flex-col gap-1">
        <p className="text-[13.5px] font-medium text-foreground">{title}</p>
        {description ? <p className="text-[12.5px] text-muted-foreground max-w-sm">{description}</p> : null}
      </div>
      {action ? <div className="mt-1">{action}</div> : null}
    </div>
  );
}

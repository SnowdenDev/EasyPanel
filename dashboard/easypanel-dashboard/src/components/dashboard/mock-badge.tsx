import { FlaskConical } from "lucide-react";
import { cn } from "@/lib/utils";

/**
 * Marks any UI backed by generated/fake data instead of a real backend call, so it never
 * gets mistaken for a live number. Pair with a `title` explaining exactly what's missing —
 * see docs/BACKEND_REQUIREMENTS.md for the endpoint each instance of this maps to.
 */
export function MockBadge({ label = "Preview data", title, className }: { label?: string; title: string; className?: string }) {
  return (
    <span
      title={title}
      className={cn(
        "inline-flex items-center gap-1 px-1.5 py-0.5 rounded-sm border border-warning/30 bg-warning/10 text-warning text-[10px] font-medium leading-none cursor-help",
        className,
      )}
    >
      <FlaskConical size={10} />
      {label}
    </span>
  );
}

import { redirect } from "next/navigation";
import {
  ScrollText,
  PlusCircle,
  Trash2,
  Pencil,
  Power,
  PowerOff,
  LogIn,
  LogOut,
  AlertTriangle,
  Activity,
} from "lucide-react";
import { backendFetch } from "@/lib/api";
import { getServerSession } from "@/lib/session";
import { Topbar } from "@/components/dashboard/topbar";
import { PageHeader } from "@/components/dashboard/page-header";
import { EmptyState } from "@/components/dashboard/empty-state";
import { Badge } from "@/components/ui/badge";
import type { AuditLogEntrySummary } from "@/lib/types";

type AuditCategory = {
  label: string;
  icon: React.ComponentType<{ className?: string }>;
  badgeClassName: string;
  iconClassName: string;
};

const CATEGORY_RULES: { match: RegExp; category: AuditCategory }[] = [
  {
    match: /create|register|new/i,
    category: {
      label: "Create",
      icon: PlusCircle,
      badgeClassName: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
      iconClassName: "bg-emerald-500/10 text-emerald-600 dark:text-emerald-400",
    },
  },
  {
    match: /delete|remove/i,
    category: {
      label: "Delete",
      icon: Trash2,
      badgeClassName: "bg-destructive/10 text-destructive",
      iconClassName: "bg-destructive/10 text-destructive",
    },
  },
  {
    match: /update|edit|rename/i,
    category: {
      label: "Update",
      icon: Pencil,
      badgeClassName: "bg-blue-500/10 text-blue-600 dark:text-blue-400",
      iconClassName: "bg-blue-500/10 text-blue-600 dark:text-blue-400",
    },
  },
  {
    match: /start|resume/i,
    category: {
      label: "Start",
      icon: Power,
      badgeClassName: "bg-primary/10 text-primary",
      iconClassName: "bg-primary/10 text-primary",
    },
  },
  {
    match: /stop|crash|kill/i,
    category: {
      label: "Stop",
      icon: PowerOff,
      badgeClassName: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
      iconClassName: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
    },
  },
  {
    match: /login|signin/i,
    category: {
      label: "Auth",
      icon: LogIn,
      badgeClassName: "bg-secondary text-secondary-foreground",
      iconClassName: "bg-secondary text-secondary-foreground",
    },
  },
  {
    match: /logout|signout/i,
    category: {
      label: "Auth",
      icon: LogOut,
      badgeClassName: "bg-secondary text-secondary-foreground",
      iconClassName: "bg-secondary text-secondary-foreground",
    },
  },
  {
    match: /fail|denied|refused|mismatch/i,
    category: {
      label: "Warning",
      icon: AlertTriangle,
      badgeClassName: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
      iconClassName: "bg-amber-500/10 text-amber-600 dark:text-amber-400",
    },
  },
];

const DEFAULT_CATEGORY: AuditCategory = {
  label: "System",
  icon: Activity,
  badgeClassName: "bg-muted text-muted-foreground",
  iconClassName: "bg-muted text-muted-foreground",
};

function categorize(action: string): AuditCategory {
  const rule = CATEGORY_RULES.find((r) => r.match.test(action));
  return rule?.category ?? DEFAULT_CATEGORY;
}

function formatTimestamp(iso: string) {
  const date = new Date(iso);
  return {
    date: date.toLocaleDateString(undefined, { month: "short", day: "numeric" }),
    time: date.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit" }),
  };
}

export default async function AuditLogPage() {
  const session = await getServerSession();
  if (session!.user.role !== "Admin") {
    redirect("/instances");
  }

  const entries = await backendFetch<AuditLogEntrySummary[]>("/api/audit-log?take=100");

  return (
    <>
      <Topbar title="Audit Log" user={session!.user} allSystemsNormal />
      <div className="flex-1 overflow-auto">
        <PageHeader title="Audit Log" description={`Most recent ${entries.length} entries across your fleet.`} />

        <div className="flex flex-col gap-5 px-8 pb-10">
          {entries.length === 0 ? (
            <div className="rounded-lg border border-border">
              <EmptyState icon={ScrollText} title="No entries yet" description="Actions across your fleet will show up here." />
            </div>
          ) : (
            <div className="rounded-lg border border-border overflow-hidden">
              <div className="hidden md:flex items-center gap-4 px-5 py-2.5 bg-muted/40 border-b border-border text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">
                <div className="w-9" />
                <div className="w-[150px]">When</div>
                <div className="flex-1">Action</div>
                <div className="w-[200px]">Actor</div>
              </div>

              <div className="flex flex-col">
                {entries.map((entry) => {
                  const category = categorize(entry.action);
                  const Icon = category.icon;
                  const { date, time } = formatTimestamp(entry.createdAtUtc);
                  return (
                    <div
                      key={entry.id}
                      className="flex items-center gap-4 px-5 py-3.5 border-b border-border last:border-b-0 hover:bg-muted/30 transition-colors"
                    >
                      <div className={`flex size-9 shrink-0 items-center justify-center rounded-full ${category.iconClassName}`}>
                        <Icon className="size-4" />
                      </div>
                      <div className="w-[150px] shrink-0 flex flex-col gap-0.5">
                        <span className="text-[13px] font-medium">{date}</span>
                        <span className="text-[11.5px] font-mono text-muted-foreground">{time}</span>
                      </div>
                      <div className="flex-1 flex flex-col gap-1 min-w-0">
                        <span className="text-[13px] font-medium truncate">{entry.action}</span>
                        <Badge variant="ghost" className={`self-start ${category.badgeClassName}`}>
                          {category.label}
                        </Badge>
                      </div>
                      <div className="w-[200px] shrink-0 text-[12.3px] font-mono text-muted-foreground truncate text-right md:text-left">
                        {entry.actorUserId ?? "system / daemon"}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}
        </div>
      </div>
    </>
  );
}

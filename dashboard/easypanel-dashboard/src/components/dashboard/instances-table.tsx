"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Play, Square, RotateCw, Search } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { StatusBadge } from "@/components/dashboard/status-badge";
import { startInstanceAction, stopInstanceAction, restartInstanceAction } from "@/lib/actions/instances";
import type { InstanceSummary } from "@/lib/types";

type PendingAction = "start" | "stop" | "restart" | null;

function InstanceRowActions({ instance }: { instance: InstanceSummary }) {
  const router = useRouter();
  const [pending, setPending] = useState<PendingAction>(null);
  const [, startTransition] = useTransition();

  function run(action: PendingAction, task: () => Promise<{ error?: string }>) {
    setPending(action);
    startTransition(async () => {
      const result = await task();
      setPending(null);
      if (result.error) {
        toast.error(result.error);
      }
      router.refresh();
    });
  }

  const canStart = instance.status === "Stopped" || instance.status === "Crashed";
  const canStop = instance.status === "Running" || instance.status === "Starting";
  const canRestart = instance.status === "Running";

  return (
    <div className="flex items-center gap-2 justify-end">
      {canStart ? (
        <Button
          variant="success"
          className="w-[92px]"
          disabled={pending !== null}
          onClick={() => run("start", () => startInstanceAction(instance.id))}
        >
          {pending === "start" ? <Loader2 className="animate-spin" /> : <Play />}
          Start
        </Button>
      ) : (
        <Button variant="ghost" className="w-[92px] text-muted-foreground" disabled>
          <Play />
          Start
        </Button>
      )}

      {canStop ? (
        <Button
          variant="destructive"
          className="w-[92px]"
          disabled={pending !== null}
          onClick={() => run("stop", () => stopInstanceAction(instance.id, false))}
        >
          {pending === "stop" ? <Loader2 className="animate-spin" /> : <Square />}
          Stop
        </Button>
      ) : null}

      {canRestart ? (
        <Button
          variant="warning"
          className="w-[100px]"
          disabled={pending !== null}
          onClick={() => run("restart", () => restartInstanceAction(instance.id))}
        >
          {pending === "restart" ? <Loader2 className="animate-spin" /> : <RotateCw />}
          Restart
        </Button>
      ) : (
        <Button variant="ghost" className="w-[100px] text-muted-foreground" disabled>
          <RotateCw />
          Restart
        </Button>
      )}
    </div>
  );
}

export function InstancesTable({ instances }: { instances: InstanceSummary[] }) {
  const router = useRouter();
  const [query, setQuery] = useState("");
  const filtered = instances.filter((instance) =>
    instance.displayName.toLowerCase().includes(query.toLowerCase()),
  );

  return (
    <div className="flex flex-col gap-5">
      <div className="flex items-center gap-2.5 px-3 py-2 rounded-md border border-border w-[280px]">
        <Search size={14} className="text-muted-foreground shrink-0" />
        <Input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Search instances..."
          aria-label="Search instances"
          className="h-auto border-none bg-transparent p-0 shadow-none focus-visible:ring-0 text-[12.8px]"
        />
      </div>

      <div className="rounded-lg border border-border overflow-hidden">
        <div className="flex items-center gap-4 px-4.5 py-2.5 bg-muted/60 border-b border-border text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">
          <div className="flex-1">Server</div>
          <div className="w-[150px]">Node</div>
          <div className="w-[320px] text-right">Actions</div>
        </div>

        {filtered.length === 0 ? (
          <div className="px-4.5 py-8 text-center text-[13px] text-muted-foreground">
            No instances match &ldquo;{query}&rdquo;.
          </div>
        ) : (
          filtered.map((instance) => (
            <div
              key={instance.id}
              role="link"
              tabIndex={0}
              onClick={() => router.push(`/instances/${instance.id}/console`)}
              onKeyDown={(event) => {
                if (event.key === "Enter") router.push(`/instances/${instance.id}/console`);
              }}
              className="flex items-center gap-4 px-4.5 min-h-10 py-3 border-b border-border last:border-b-0 hover:bg-muted/60 transition-colors cursor-pointer"
            >
              <div className="flex flex-col gap-1.5 flex-1 min-w-0">
                <span className="font-mono text-[13px] font-medium text-foreground">
                  {instance.displayName}
                </span>
                <StatusBadge status={instance.status} />
              </div>
              <div className="w-[150px] text-[12.8px] text-muted-foreground truncate">
                {instance.nodeDisplayName}
              </div>
              <div className="w-[320px]" onClick={(event) => event.stopPropagation()}>
                <InstanceRowActions instance={instance} />
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}

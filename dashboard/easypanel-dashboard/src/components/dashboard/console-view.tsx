"use client";

import { useEffect, useRef, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Send, Wifi, WifiOff } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { StatusBadge } from "@/components/dashboard/status-badge";
import { startInstanceAction, stopInstanceAction, restartInstanceAction } from "@/lib/actions/instances";
import type { InstanceStatus } from "@/lib/types";

interface ConsoleLine {
  key: string;
  streamKind: "StandardOutput" | "StandardError";
  text: string;
  timestampUtc: string;
}

export function ConsoleView({
  instanceId,
  initialStatus,
}: {
  instanceId: string;
  initialStatus: InstanceStatus;
}) {
  const router = useRouter();
  const [status, setStatus] = useState<InstanceStatus>(initialStatus);
  const [lines, setLines] = useState<ConsoleLine[]>([]);
  const [connected, setConnected] = useState(false);
  const [command, setCommand] = useState("");
  const [powerPending, setPowerPending] = useState<"start" | "stop" | "restart" | null>(null);
  const [, startTransition] = useTransition();
  const scrollRef = useRef<HTMLDivElement>(null);
  const nextKey = useRef(0);

  useEffect(() => {
    // A plain SSE connection to our own server — see app/api/console-stream/[instanceId]:
    // the actual SignalR connection to the backend lives there, never in this browser.
    const source = new EventSource(`/api/console-stream/${instanceId}`);

    source.addEventListener("console-output", (event) => {
      const data = JSON.parse((event as MessageEvent).data) as {
        streamKind: ConsoleLine["streamKind"];
        text: string;
        timestampUtc: string;
      };
      setLines((prev) => [...prev, { key: String(nextKey.current++), ...data }]);
    });

    source.addEventListener("status-changed", (event) => {
      const data = JSON.parse((event as MessageEvent).data) as { status: string };
      setStatus(data.status as InstanceStatus);
      router.refresh();
    });

    source.addEventListener("connection-state", (event) => {
      const data = JSON.parse((event as MessageEvent).data) as { connected: boolean };
      setConnected(data.connected);
    });

    source.onerror = () => setConnected(false);

    return () => source.close();
  }, [instanceId, router]);

  useEffect(() => {
    scrollRef.current?.scrollTo({ top: scrollRef.current.scrollHeight });
  }, [lines]);

  async function handleSend(event: React.FormEvent) {
    event.preventDefault();
    if (!command.trim()) return;

    const commandText = command;
    setCommand("");
    try {
      const response = await fetch(`/api/console-stream/${instanceId}/command`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ command: commandText }),
      });
      if (!response.ok) {
        const body = (await response.json().catch(() => ({}))) as { message?: string };
        toast.error(body.message ?? "Failed to send the command.");
      }
    } catch {
      toast.error("Failed to send the command.");
    }
  }

  function runPower(action: "start" | "stop" | "restart", task: () => Promise<{ error?: string }>) {
    setPowerPending(action);
    startTransition(async () => {
      const result = await task();
      setPowerPending(null);
      if (result.error) toast.error(result.error);
      router.refresh();
    });
  }

  return (
    <div className="flex flex-col gap-4.5 flex-1 min-h-0">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <StatusBadge status={status} />
          <span className="flex items-center gap-1 text-xs text-muted-foreground">
            {connected ? <Wifi size={13} className="text-success" /> : <WifiOff size={13} />}
            {connected ? "Live" : "Connecting…"}
          </span>
        </div>
        <div className="flex items-center gap-2">
          {status === "Stopped" || status === "Crashed" ? (
            <Button
              variant="success"
              className="w-[92px]"
              disabled={powerPending !== null}
              onClick={() => runPower("start", () => startInstanceAction(instanceId))}
            >
              {powerPending === "start" ? <Loader2 className="animate-spin" /> : null}
              Start
            </Button>
          ) : (
            <Button
              variant="destructive"
              className="w-[92px]"
              disabled={powerPending !== null}
              onClick={() => runPower("stop", () => stopInstanceAction(instanceId, false))}
            >
              {powerPending === "stop" ? <Loader2 className="animate-spin" /> : null}
              Stop
            </Button>
          )}
          <Button
            variant="warning"
            className="w-[100px]"
            disabled={powerPending !== null || status !== "Running"}
            onClick={() => runPower("restart", () => restartInstanceAction(instanceId))}
          >
            {powerPending === "restart" ? <Loader2 className="animate-spin" /> : null}
            Restart
          </Button>
        </div>
      </div>

      <div className="flex flex-col flex-1 min-h-0 bg-black/40 border border-border rounded-lg overflow-hidden">
        <div ref={scrollRef} className="flex-1 overflow-auto px-4.5 py-4 font-mono text-[12.3px] leading-relaxed">
          {lines.length === 0 ? (
            <p className="text-muted-foreground">Waiting for output…</p>
          ) : (
            lines.map((line) => (
              <div key={line.key}>
                <span className="text-muted-foreground">
                  {new Date(line.timestampUtc).toLocaleTimeString()}
                </span>{" "}
                <span className={line.streamKind === "StandardError" ? "text-destructive" : "text-foreground/80"}>
                  {line.text}
                </span>
              </div>
            ))
          )}
        </div>
        <form onSubmit={handleSend} className="flex items-center gap-2.5 px-4 py-2.5 border-t border-border shrink-0">
          <span className="font-mono text-[13px] text-primary shrink-0">$</span>
          <Input
            value={command}
            onChange={(e) => setCommand(e.target.value)}
            placeholder="Type a command and press Enter"
            aria-label="Console command input"
            className="flex-1 border-none bg-transparent shadow-none px-0 h-auto focus-visible:ring-0 font-mono text-[12.5px]"
          />
          <Button type="submit" size="icon" variant="ghost" aria-label="Send command" title="Send command">
            <Send size={14} />
          </Button>
        </form>
      </div>
    </div>
  );
}

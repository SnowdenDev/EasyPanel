"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Check, Copy, Loader2, Plus } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { registerNodeAction } from "@/lib/actions/nodes";
import type { NodeConnectivityMode, RegisterNodeResult } from "@/lib/types";

export function RegisterNodeDialog() {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [displayName, setDisplayName] = useState("");
  const [connectivityMode, setConnectivityMode] = useState<NodeConnectivityMode>("Remote");
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<RegisterNodeResult | null>(null);
  const [copied, setCopied] = useState(false);

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    startTransition(async () => {
      const outcome = await registerNodeAction(displayName, connectivityMode);
      if (outcome.error) {
        setError(outcome.error);
        return;
      }
      setResult(outcome.result ?? null);
    });
  }

  function handleClose(next: boolean) {
    setOpen(next);
    if (!next) {
      setDisplayName("");
      setConnectivityMode("Remote");
      setError(null);
      setResult(null);
      setCopied(false);
      router.refresh();
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogTrigger asChild>
        <Button>
          <Plus />
          Register Node
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[440px]">
        {result ? (
          <>
            <DialogHeader>
              <DialogTitle>Node registered</DialogTitle>
              <DialogDescription>
                This token is shown once — the backend only ever stores its hash after this.
                Put it straight into the daemon&apos;s config on that node.
              </DialogDescription>
            </DialogHeader>
            <div className="flex flex-col gap-3">
              <div className="flex flex-col gap-1">
                <Label>Node ID</Label>
                <code className="text-xs font-mono px-2.5 py-2 rounded-md bg-muted break-all">
                  {result.nodeId}
                </code>
              </div>
              <div className="flex flex-col gap-1">
                <Label>Raw node token</Label>
                <div className="flex gap-2">
                  <code className="flex-1 text-xs font-mono px-2.5 py-2 rounded-md bg-muted break-all">
                    {result.rawNodeToken}
                  </code>
                  <Button
                    type="button"
                    variant="secondary"
                    size="icon"
                    onClick={async () => {
                      await navigator.clipboard.writeText(result.rawNodeToken);
                      setCopied(true);
                      toast.success("Copied.");
                    }}
                  >
                    {copied ? <Check /> : <Copy />}
                  </Button>
                </div>
              </div>
            </div>
            <DialogFooter>
              <Button onClick={() => handleClose(false)}>Done</Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>Register a node</DialogTitle>
              <DialogDescription>
                Set connectivity mode honestly — Local lifts the file-manager size cap,
                Remote caps transfers at 10 MB.
              </DialogDescription>
            </DialogHeader>
            <form onSubmit={handleSubmit} className="flex flex-col gap-4">
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="displayName">Display name</Label>
                <Input
                  id="displayName"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  required
                  autoFocus
                />
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="connectivityMode">Connectivity mode</Label>
                <Select value={connectivityMode} onValueChange={(v) => setConnectivityMode(v as NodeConnectivityMode)}>
                  <SelectTrigger id="connectivityMode" className="w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="Local">Local / LAN — no file size cap</SelectItem>
                    <SelectItem value="Remote">Remote — 10 MB file cap</SelectItem>
                  </SelectContent>
                </Select>
              </div>
              {error ? <p className="text-[13px] text-destructive">{error}</p> : null}
              <DialogFooter>
                <Button type="submit" disabled={isPending}>
                  {isPending ? <Loader2 className="animate-spin" /> : null}
                  Register
                </Button>
              </DialogFooter>
            </form>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}

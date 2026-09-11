"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Save } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { updateNodeAction } from "@/lib/actions/nodes";
import type { NodeConnectivityMode, NodeSummary } from "@/lib/types";

export function NodeEditForm({ node, onSuccess }: { node: NodeSummary; onSuccess?: () => void }) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [displayName, setDisplayName] = useState(node.displayName);
  const [connectivityMode, setConnectivityMode] = useState<NodeConnectivityMode>(node.connectivityMode);

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    startTransition(async () => {
      const result = await updateNodeAction(node.id, displayName, connectivityMode);
      if (result.error) {
        toast.error(result.error);
        return;
      }
      toast.success("Node updated.");
      router.refresh();
      onSuccess?.();
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 max-w-md">
      <div className="flex flex-col gap-1.5">
        <Label htmlFor="displayName">Display name</Label>
        <Input id="displayName" value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
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
        <p className="text-xs text-muted-foreground">
          Changing this only affects file transfers going forward — it doesn&apos;t touch
          anything already in flight.
        </p>
      </div>

      <Button type="submit" disabled={isPending} className="self-start">
        {isPending ? <Loader2 className="animate-spin" /> : <Save />}
        Save changes
      </Button>
    </form>
  );
}

"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Plus, Wand2 } from "lucide-react";
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
import { createInstanceAction, computeExecutableHashAction } from "@/lib/actions/instances";
import type { NodeSummary } from "@/lib/types";

export function NewInstanceDialog({ nodes }: { nodes: NodeSummary[] }) {
  const router = useRouter();
  const [open, setOpen] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [isHashing, setIsHashing] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [nodeId, setNodeId] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [workDirectory, setWorkDirectory] = useState("");
  const [executableRelativePath, setExecutableRelativePath] = useState("");
  const [expectedExecutableSha256, setExpectedExecutableSha256] = useState("");
  const [launchArguments, setLaunchArguments] = useState("");

  function resetForm() {
    setNodeId("");
    setDisplayName("");
    setWorkDirectory("");
    setExecutableRelativePath("");
    setExpectedExecutableSha256("");
    setLaunchArguments("");
    setError(null);
  }

  async function handleComputeHash() {
    if (!nodeId || !workDirectory || !executableRelativePath) {
      toast.error("Node, work directory, and executable path are required to compute a hash.");
      return;
    }
    setIsHashing(true);
    const result = await computeExecutableHashAction(nodeId, workDirectory, executableRelativePath);
    setIsHashing(false);
    if (result.error) {
      toast.error(result.error);
      return;
    }
    setExpectedExecutableSha256(result.sha256 ?? "");
    toast.success("Hash computed from the node's own disk.");
  }

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setError(null);
    startTransition(async () => {
      const result = await createInstanceAction({
        nodeId,
        displayName,
        workDirectory,
        executableRelativePath,
        expectedExecutableSha256,
        launchArguments,
      });
      if (result.error) {
        setError(result.error);
        return;
      }
      setOpen(false);
      resetForm();
      router.refresh();
    });
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(next) => {
        setOpen(next);
        if (!next) resetForm();
      }}
    >
      <DialogTrigger asChild>
        <Button>
          <Plus />
          New Instance
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>New instance</DialogTitle>
          <DialogDescription>
            The daemon refuses to launch if the executable&apos;s hash doesn&apos;t match this exactly.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="nodeId">Node</Label>
            <Select value={nodeId} onValueChange={setNodeId}>
              <SelectTrigger id="nodeId" className="w-full">
                <SelectValue placeholder="Select a node" />
              </SelectTrigger>
              <SelectContent>
                {nodes.map((node) => (
                  <SelectItem key={node.id} value={node.id}>
                    {node.displayName} {node.isOnline ? "" : "(offline)"}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="displayName">Display name</Label>
            <Input id="displayName" value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="workDirectory">Work directory</Label>
            <Input
              id="workDirectory"
              placeholder="C:\Servers\myserver"
              value={workDirectory}
              onChange={(e) => setWorkDirectory(e.target.value)}
              required
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="executableRelativePath">Executable (relative to work directory)</Label>
            <Input
              id="executableRelativePath"
              placeholder="server.exe"
              value={executableRelativePath}
              onChange={(e) => setExecutableRelativePath(e.target.value)}
              required
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="expectedExecutableSha256">Expected SHA-256</Label>
            <div className="flex gap-2">
              <Input
                id="expectedExecutableSha256"
                className="font-mono text-xs"
                value={expectedExecutableSha256}
                onChange={(e) => setExpectedExecutableSha256(e.target.value)}
                required
              />
              <Button type="button" variant="secondary" onClick={handleComputeHash} disabled={isHashing}>
                {isHashing ? <Loader2 className="animate-spin" /> : <Wand2 />}
                Compute
              </Button>
            </div>
            <p className="text-xs text-muted-foreground">
              Asks the node to hash the file on its own disk — the node must be online.
            </p>
          </div>

          <div className="flex flex-col gap-1.5">
            <Label htmlFor="launchArguments">Launch arguments (optional)</Label>
            <Input
              id="launchArguments"
              value={launchArguments}
              onChange={(e) => setLaunchArguments(e.target.value)}
            />
          </div>

          {error ? <p className="text-[13px] text-destructive">{error}</p> : null}

          <DialogFooter>
            <Button type="submit" disabled={isPending}>
              {isPending ? <Loader2 className="animate-spin" /> : null}
              Create instance
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

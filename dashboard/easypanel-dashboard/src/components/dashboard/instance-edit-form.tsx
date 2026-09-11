"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { Loader2, Save, Wand2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { FormSection } from "@/components/dashboard/form-section";
import { updateInstanceAction, computeExecutableHashAction } from "@/lib/actions/instances";
import type { InstanceStatusDetails } from "@/lib/types";

export function InstanceEditForm({
  instance,
  onSuccess,
}: {
  instance: InstanceStatusDetails;
  onSuccess?: () => void;
}) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [isHashing, setIsHashing] = useState(false);

  const [displayName, setDisplayName] = useState(instance.displayName);
  const [workDirectory, setWorkDirectory] = useState(instance.workDirectory);
  const [executableRelativePath, setExecutableRelativePath] = useState(instance.executableRelativePath);
  const [expectedExecutableSha256, setExpectedExecutableSha256] = useState(instance.expectedExecutableSha256);
  const [launchArguments, setLaunchArguments] = useState(instance.launchArguments ?? "");
  const [cpuLimitPercent, setCpuLimitPercent] = useState(
    instance.cpuLimitPercent != null ? String(instance.cpuLimitPercent) : "",
  );
  const [memoryLimitMegabytes, setMemoryLimitMegabytes] = useState(
    instance.memoryLimitMegabytes != null ? String(instance.memoryLimitMegabytes) : "",
  );

  async function handleComputeHash() {
    if (!workDirectory || !executableRelativePath) {
      toast.error("Work directory and executable path are required to compute a hash.");
      return;
    }
    setIsHashing(true);
    const result = await computeExecutableHashAction(instance.nodeId, workDirectory, executableRelativePath);
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
    startTransition(async () => {
      const result = await updateInstanceAction(instance.instanceId, {
        displayName,
        workDirectory,
        executableRelativePath,
        expectedExecutableSha256,
        launchArguments,
        cpuLimitPercent: cpuLimitPercent ? Number(cpuLimitPercent) : undefined,
        memoryLimitMegabytes: memoryLimitMegabytes ? Number(memoryLimitMegabytes) : undefined,
      });
      if (result.error) {
        toast.error(result.error);
        return;
      }
      toast.success("Instance updated.");
      router.refresh();
      onSuccess?.();
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-5 max-w-md">
      <p className="text-[13px] text-muted-foreground bg-muted/50 rounded-md px-3 py-2">
        Changes apply the next time this instance is started — they don&apos;t touch a
        currently running process.
      </p>

      <FormSection title="Identity">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="displayName">Display name</Label>
          <Input id="displayName" value={displayName} onChange={(e) => setDisplayName(e.target.value)} required />
        </div>
      </FormSection>

      <Separator />

      <FormSection title="Executable" description="Where the daemon finds and verifies the process on disk.">
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="workDirectory">Work directory</Label>
          <Input
            id="workDirectory"
            value={workDirectory}
            onChange={(e) => setWorkDirectory(e.target.value)}
            required
          />
        </div>

        <div className="flex flex-col gap-1.5">
          <Label htmlFor="executableRelativePath">Executable (relative to work directory)</Label>
          <Input
            id="executableRelativePath"
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
          <Input id="launchArguments" value={launchArguments} onChange={(e) => setLaunchArguments(e.target.value)} />
        </div>
      </FormSection>

      <Separator />

      <FormSection title="Resource limits" description="Leave blank to run without a cap.">
        <div className="grid grid-cols-2 gap-4">
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="cpuLimitPercent">CPU limit %</Label>
            <Input
              id="cpuLimitPercent"
              type="number"
              min={1}
              max={100}
              placeholder="No cap"
              value={cpuLimitPercent}
              onChange={(e) => setCpuLimitPercent(e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="memoryLimitMegabytes">Memory limit MB</Label>
            <Input
              id="memoryLimitMegabytes"
              type="number"
              min={1}
              placeholder="No cap"
              value={memoryLimitMegabytes}
              onChange={(e) => setMemoryLimitMegabytes(e.target.value)}
            />
          </div>
        </div>
      </FormSection>

      <Button type="submit" disabled={isPending} className="self-start">
        {isPending ? <Loader2 className="animate-spin" /> : <Save />}
        Save changes
      </Button>
    </form>
  );
}

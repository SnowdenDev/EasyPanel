"use client";

import { useState, useTransition } from "react";
import { Loader2, Save } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { assignServerPermissionsAction } from "@/lib/actions/staff";
import type { InstancePermissionSummary, ServerPermissions } from "@/lib/types";

const permissionFields: { key: keyof ServerPermissions; label: string }[] = [
  { key: "canViewConsole", label: "View console" },
  { key: "canSendConsoleInput", label: "Console input" },
  { key: "canControlPower", label: "Power" },
  { key: "canAccessFileManager", label: "Files" },
  { key: "canEditSettings", label: "Settings" },
];

function PermissionMatrixRow({ userId, instance }: { userId: string; instance: InstancePermissionSummary }) {
  const [isPending, startTransition] = useTransition();
  const [permissions, setPermissions] = useState<ServerPermissions>({
    canViewConsole: instance.canViewConsole,
    canSendConsoleInput: instance.canSendConsoleInput,
    canControlPower: instance.canControlPower,
    canAccessFileManager: instance.canAccessFileManager,
    canEditSettings: instance.canEditSettings,
  });

  function handleSave() {
    startTransition(async () => {
      const result = await assignServerPermissionsAction(userId, instance.instanceId, permissions);
      if (result.error) {
        toast.error(result.error);
        return;
      }
      toast.success(`Permissions saved for ${instance.instanceDisplayName}.`);
    });
  }

  return (
    <div className="flex items-center gap-4 px-4.5 min-h-10 py-2.5 border-b border-border last:border-b-0">
      <span className="text-[13px] font-medium flex-1 min-w-0 truncate">{instance.instanceDisplayName}</span>
      {permissionFields.map((field) => (
        <label key={field.key} className="flex flex-col items-center gap-1 w-[92px] shrink-0">
          <Checkbox
            checked={permissions[field.key]}
            onCheckedChange={(checked) =>
              setPermissions((prev) => ({ ...prev, [field.key]: checked === true }))
            }
          />
          <span className="text-[10.5px] text-muted-foreground">{field.label}</span>
        </label>
      ))}
      <Button variant="secondary" size="icon" disabled={isPending} onClick={handleSave} className="shrink-0">
        {isPending ? <Loader2 className="animate-spin" /> : <Save />}
      </Button>
    </div>
  );
}

export function StaffPermissionMatrix({
  userId,
  permissions,
}: {
  userId: string;
  permissions: InstancePermissionSummary[];
}) {
  if (permissions.length === 0) {
    return (
      <p className="text-[13px] text-muted-foreground">No instances exist yet to grant access to.</p>
    );
  }

  return (
    <div className="rounded-lg border border-border overflow-hidden">
      <div className="flex items-center gap-4 px-4.5 py-2.5 bg-muted/60 border-b border-border text-[11px] font-semibold tracking-wide uppercase text-muted-foreground">
        <div className="flex-1">Instance</div>
        {permissionFields.map((field) => (
          <div key={field.key} className="w-[92px] text-center shrink-0">
            {field.label}
          </div>
        ))}
        <div className="w-10 shrink-0" />
      </div>
      {permissions.map((instance) => (
        <PermissionMatrixRow key={instance.instanceId} userId={userId} instance={instance} />
      ))}
    </div>
  );
}

"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { InstanceEditForm } from "@/components/dashboard/instance-edit-form";
import { ConfirmDeleteDialog } from "@/components/dashboard/confirm-delete-dialog";
import { deleteInstanceAction } from "@/lib/actions/instances";
import type { InstanceStatusDetails } from "@/lib/types";

export function InstanceEditDialog({
  instance,
  redirectOnDeleteTo,
}: {
  instance: InstanceStatusDetails;
  redirectOnDeleteTo?: string;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);

  const canDelete = instance.status !== "Running" && instance.status !== "Starting" && instance.status !== "Stopping";

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="secondary">
          <Pencil />
          Edit
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[480px] max-h-[85vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Edit instance</DialogTitle>
        </DialogHeader>
        <InstanceEditForm instance={instance} onSuccess={() => setOpen(false)} />

        <div className="flex items-center justify-between gap-4 pt-4 mt-2 border-t border-border">
          <p className="text-xs text-muted-foreground">
            {canDelete ? "Danger zone" : "Stop the instance to delete it"}
          </p>
          <ConfirmDeleteDialog
            trigger={
              <Button variant="destructive" disabled={!canDelete}>
                <Trash2 />
                Delete instance
              </Button>
            }
            resourceLabel="Instance"
            resourceName={instance.displayName}
            onConfirm={() => deleteInstanceAction(instance.instanceId)}
            onDeleted={() => {
              setOpen(false);
              if (redirectOnDeleteTo) {
                router.push(redirectOnDeleteTo);
              } else {
                router.refresh();
              }
            }}
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}

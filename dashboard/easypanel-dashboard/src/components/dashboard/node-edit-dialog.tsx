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
import { NodeEditForm } from "@/components/dashboard/node-edit-form";
import { ConfirmDeleteDialog } from "@/components/dashboard/confirm-delete-dialog";
import { deleteNodeAction } from "@/lib/actions/nodes";
import type { NodeSummary } from "@/lib/types";

export function NodeEditDialog({
  node,
  redirectOnDeleteTo,
  trigger,
}: {
  node: NodeSummary;
  redirectOnDeleteTo?: string;
  trigger?: React.ReactNode;
}) {
  const router = useRouter();
  const [open, setOpen] = useState(false);

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        {trigger ?? (
          <Button variant="secondary" size="icon">
            <Pencil />
          </Button>
        )}
      </DialogTrigger>
      <DialogContent className="sm:max-w-[480px]">
        <DialogHeader>
          <DialogTitle>Edit node</DialogTitle>
        </DialogHeader>
        <NodeEditForm node={node} onSuccess={() => setOpen(false)} />

        <div className="flex items-center justify-between gap-4 pt-4 mt-2 border-t border-border">
          <p className="text-xs text-muted-foreground">Danger zone</p>
          <ConfirmDeleteDialog
            trigger={
              <Button variant="destructive">
                <Trash2 />
                Delete node
              </Button>
            }
            resourceLabel="Node"
            resourceName={node.displayName}
            description="This deletes every instance registered on this node too."
            onConfirm={() => deleteNodeAction(node.id)}
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

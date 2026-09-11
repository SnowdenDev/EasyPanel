"use client";

import { useState, useTransition, type ReactNode } from "react";
import { Loader2, Trash2 } from "lucide-react";
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

export function ConfirmDeleteDialog({
  trigger,
  resourceLabel,
  resourceName,
  description,
  onConfirm,
  onDeleted,
}: {
  trigger: ReactNode;
  resourceLabel: string;
  resourceName: string;
  description?: string;
  onConfirm: () => Promise<{ error?: string }>;
  onDeleted?: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [isPending, startTransition] = useTransition();
  const [confirmText, setConfirmText] = useState("");

  const isMatch = confirmText === resourceName || confirmText.trim().toUpperCase() === "DELETE";

  function handleClose(next: boolean) {
    setOpen(next);
    if (!next) {
      setConfirmText("");
    }
  }

  function handleConfirm() {
    startTransition(async () => {
      const result = await onConfirm();
      if (result.error) {
        toast.error(result.error);
        return;
      }
      toast.success(`${resourceLabel} deleted.`);
      handleClose(false);
      onDeleted?.();
    });
  }

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogTrigger asChild>{trigger}</DialogTrigger>
      <DialogContent className="sm:max-w-[420px]">
        <DialogHeader>
          <DialogTitle>Delete {resourceLabel.toLowerCase()}</DialogTitle>
          <DialogDescription>
            {description ?? "This can't be undone."} Type{" "}
            <span className="font-mono font-semibold text-foreground">{resourceName}</span> or{" "}
            <span className="font-mono font-semibold text-foreground">DELETE</span> to confirm.
          </DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-1.5">
          <Label htmlFor="confirmText">Confirmation</Label>
          <Input
            id="confirmText"
            autoFocus
            autoComplete="off"
            value={confirmText}
            onChange={(e) => setConfirmText(e.target.value)}
          />
        </div>
        <DialogFooter>
          <Button variant="secondary" onClick={() => handleClose(false)}>
            Cancel
          </Button>
          <Button variant="destructive" disabled={!isMatch || isPending} onClick={handleConfirm}>
            {isPending ? <Loader2 className="animate-spin" /> : <Trash2 />}
            Delete {resourceLabel.toLowerCase()}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

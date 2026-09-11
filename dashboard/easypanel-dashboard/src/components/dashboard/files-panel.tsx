"use client";

import { useMemo, useRef, useState } from "react";
import { ChevronRight, Download, File, Folder, Loader2, Upload } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { MockBadge } from "@/components/dashboard/mock-badge";
import { generateMockFileTree, type MockFileEntry } from "@/lib/mock-data";
import { cn } from "@/lib/utils";

function formatBytes(bytes: number | null): string {
  if (bytes === null) return "—";
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

function FileNode({
  entry,
  depth,
  onSelectFile,
  selectedPath,
  path,
}: {
  entry: MockFileEntry;
  depth: number;
  onSelectFile: (path: string) => void;
  selectedPath: string | null;
  path: string;
}) {
  const [open, setOpen] = useState(depth < 1);

  if (entry.type === "folder") {
    return (
      <div>
        <button
          type="button"
          onClick={() => setOpen((v) => !v)}
          className="flex items-center gap-1.5 w-full px-2 py-1.5 rounded-md hover:bg-muted/60 text-left transition-colors"
          style={{ paddingLeft: `${depth * 14 + 8}px` }}
        >
          <ChevronRight size={13} className={cn("text-muted-foreground shrink-0 transition-transform", open && "rotate-90")} />
          <Folder size={14} className="text-muted-foreground shrink-0" />
          <span className="text-[12.8px] truncate">{entry.name}</span>
        </button>
        {open && entry.children ? (
          <div>
            {entry.children.map((child) => (
              <FileNode
                key={child.name}
                entry={child}
                depth={depth + 1}
                onSelectFile={onSelectFile}
                selectedPath={selectedPath}
                path={`${path}${child.name}`}
              />
            ))}
          </div>
        ) : null}
      </div>
    );
  }

  const isSelected = selectedPath === path;

  return (
    <button
      type="button"
      onClick={() => onSelectFile(path)}
      className={cn(
        "flex items-center gap-1.5 w-full px-2 py-1.5 rounded-md text-left transition-colors",
        isSelected ? "bg-primary/10 text-primary" : "hover:bg-muted/60",
      )}
      style={{ paddingLeft: `${depth * 14 + 26}px` }}
    >
      <File size={13} className="shrink-0 text-muted-foreground" />
      <span className="flex-1 text-[12.8px] truncate font-mono">{entry.name}</span>
      <span className="text-[11px] text-muted-foreground shrink-0">{formatBytes(entry.sizeBytes)}</span>
    </button>
  );
}

export function FilesPanel({ instanceId }: { instanceId: string }) {
  const [downloadPath, setDownloadPath] = useState("");
  const [uploadPath, setUploadPath] = useState("");
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const tree = useMemo(() => generateMockFileTree(instanceId), [instanceId]);

  function handleDownload() {
    if (!downloadPath.trim()) {
      toast.error("Enter the file's path, relative to the instance's work directory.");
      return;
    }
    const url = `/api/files/download?instanceId=${instanceId}&path=${encodeURIComponent(downloadPath)}`;
    window.location.href = url;
  }

  async function handleUpload() {
    const file = fileInputRef.current?.files?.[0];
    if (!file) {
      toast.error("Choose a file first.");
      return;
    }
    if (!uploadPath.trim()) {
      toast.error("Enter the destination path, relative to the instance's work directory.");
      return;
    }

    setIsUploading(true);
    try {
      const response = await fetch(
        `/api/files/upload?instanceId=${instanceId}&path=${encodeURIComponent(uploadPath)}`,
        { method: "PUT", body: file },
      );
      if (!response.ok) {
        const body = (await response.json().catch(() => ({}))) as { message?: string };
        toast.error(body.message ?? "Upload failed.");
        return;
      }
      toast.success("Uploaded.");
      setUploadPath("");
      if (fileInputRef.current) fileInputRef.current.value = "";
    } finally {
      setIsUploading(false);
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_320px] gap-5">
        <div className="flex flex-col gap-2 rounded-lg border border-border overflow-hidden">
          <div className="flex items-center justify-between px-4 py-3 border-b border-border bg-muted/40">
            <span className="text-[12.5px] font-medium">Directory browser</span>
            <MockBadge
              title="The backend has no directory-listing endpoint yet — this tree is generated locally to preview the UX. See docs/BACKEND_REQUIREMENTS.md (GET /api/files/list)."
            />
          </div>
          <div className="flex flex-col py-1.5 max-h-[360px] overflow-auto">
            {tree.children?.map((entry) => (
              <FileNode
                key={entry.name}
                entry={entry}
                depth={0}
                selectedPath={downloadPath}
                onSelectFile={(path) => setDownloadPath(path)}
                path={entry.name}
              />
            ))}
          </div>
          <p className="px-4 py-2.5 border-t border-border text-[11.5px] text-muted-foreground">
            Click a file to fill in the download path below.
          </p>
        </div>

        <div className="flex flex-col gap-5">
          <div className="flex flex-col gap-2 p-4 rounded-lg border border-border bg-card">
            <Label htmlFor="downloadPath" className="text-[12px]">Download</Label>
            <Input
              id="downloadPath"
              placeholder="logs/latest.log"
              value={downloadPath}
              onChange={(e) => setDownloadPath(e.target.value)}
            />
            <Button variant="secondary" onClick={handleDownload} className="self-start">
              <Download />
              Download
            </Button>
          </div>

          <div className="flex flex-col gap-2 p-4 rounded-lg border border-border bg-card">
            <Label htmlFor="uploadPath" className="text-[12px]">Upload to</Label>
            <Input
              id="uploadPath"
              placeholder="config/server.cfg"
              value={uploadPath}
              onChange={(e) => setUploadPath(e.target.value)}
            />
            <input ref={fileInputRef} type="file" className="text-[12.5px]" />
            <Button variant="secondary" onClick={handleUpload} disabled={isUploading} className="self-start">
              {isUploading ? <Loader2 className="animate-spin" /> : <Upload />}
              Upload
            </Button>
          </div>
        </div>
      </div>

      <p className="text-[12px] text-muted-foreground">
        Uploads/downloads themselves are real backend calls, one file at a time, by exact path
        relative to the instance&apos;s work directory. A Remote node caps transfers at 10 MB; a
        Local node has no cap.
      </p>
    </div>
  );
}

"use client";

import { useRef, useState } from "react";
import { Download, Loader2, Upload } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

export function FilesPanel({ instanceId }: { instanceId: string }) {
  const [downloadPath, setDownloadPath] = useState("");
  const [uploadPath, setUploadPath] = useState("");
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

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
    <div className="flex flex-col gap-6 max-w-md">
      <p className="text-[13px] text-muted-foreground">
        The backend doesn&apos;t expose a directory listing yet — one file at a time, by
        its exact path relative to the instance&apos;s work directory. A Remote node caps
        uploads/downloads at 10 MB; a Local node has no cap.
      </p>

      <div className="flex flex-col gap-2">
        <Label htmlFor="downloadPath">Download</Label>
        <div className="flex gap-2">
          <Input
            id="downloadPath"
            placeholder="logs/latest.log"
            value={downloadPath}
            onChange={(e) => setDownloadPath(e.target.value)}
          />
          <Button variant="secondary" onClick={handleDownload}>
            <Download />
            Download
          </Button>
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Label htmlFor="uploadPath">Upload to</Label>
        <Input
          id="uploadPath"
          placeholder="config/server.cfg"
          value={uploadPath}
          onChange={(e) => setUploadPath(e.target.value)}
        />
        <input ref={fileInputRef} type="file" className="text-[13px]" />
        <Button variant="secondary" onClick={handleUpload} disabled={isUploading} className="self-start">
          {isUploading ? <Loader2 className="animate-spin" /> : <Upload />}
          Upload
        </Button>
      </div>
    </div>
  );
}

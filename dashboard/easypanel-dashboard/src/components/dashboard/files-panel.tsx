"use client";

import { useMemo, useRef, useState } from "react";
import CodeMirror from "@uiw/react-codemirror";
import { json } from "@codemirror/lang-json";
import { javascript } from "@codemirror/lang-javascript";
import { yaml } from "@codemirror/lang-yaml";
import { oneDark } from "@codemirror/theme-one-dark";
import {
  AppWindow,
  Blocks,
  ChevronRight,
  Circle,
  Database,
  Download,
  File,
  FileArchive,
  FileCode2,
  FileJson,
  FileText,
  FileWarning,
  Folder,
  FolderOpen,
  Loader2,
  RotateCcw,
  Save,
  ScrollText,
  Settings2,
  Upload,
  X,
  type LucideIcon,
} from "lucide-react";
import { useTheme } from "next-themes";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { MockBadge } from "@/components/dashboard/mock-badge";
import { EmptyState } from "@/components/dashboard/empty-state";
import { generateMockFileContent, generateMockFileTree, type MockFileEntry } from "@/lib/mock-data";
import { cn } from "@/lib/utils";

function formatBytes(bytes: number | null): string {
  if (bytes === null) return "—";
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

function languageExtension(path: string) {
  const ext = path.split(".").pop()?.toLowerCase();
  switch (ext) {
    case "json":
      return json();
    case "yml":
    case "yaml":
      return yaml();
    case "js":
    case "ts":
    case "cjs":
    case "mjs":
      return javascript();
    default:
      return null;
  }
}

interface OpenTab {
  path: string;
  original: string;
  draft: string;
}

interface FileIconMeta {
  icon: LucideIcon;
  className: string;
}

/**
 * Maps a filename to a distinct icon + color so binaries (.exe, .dll), archives, data
 * files, and config/text files are visually distinguishable at a glance — like a
 * code editor's file tree.
 */
function getFileIconMeta(name: string): FileIconMeta {
  const ext = name.split(".").pop()?.toLowerCase() ?? "";

  switch (ext) {
    case "exe":
      return { icon: AppWindow, className: "text-blue-500 dark:text-blue-400" };
    case "dll":
    case "so":
    case "dylib":
      return { icon: Blocks, className: "text-violet-500 dark:text-violet-400" };
    case "json":
      return { icon: FileJson, className: "text-amber-500 dark:text-amber-400" };
    case "yml":
    case "yaml":
      return { icon: FileCode2, className: "text-emerald-500 dark:text-emerald-400" };
    case "log":
      return { icon: ScrollText, className: "text-muted-foreground" };
    case "cfg":
    case "ini":
    case "conf":
      return { icon: Settings2, className: "text-cyan-500 dark:text-cyan-400" };
    case "dat":
    case "dat_old":
    case "db":
      return { icon: Database, className: "text-orange-500 dark:text-orange-400" };
    case "zip":
    case "jar":
    case "tar":
    case "gz":
      return { icon: FileArchive, className: "text-yellow-600 dark:text-yellow-500" };
    case "txt":
    case "md":
      return { icon: FileText, className: "text-muted-foreground" };
    default:
      return { icon: File, className: "text-muted-foreground" };
  }
}

function FileNode({
  entry,
  depth,
  onSelectFile,
  activePath,
  path,
}: {
  entry: MockFileEntry;
  depth: number;
  onSelectFile: (path: string) => void;
  activePath: string | null;
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
          {open ? (
            <FolderOpen size={14} className="text-blue-500 dark:text-blue-400 shrink-0" />
          ) : (
            <Folder size={14} className="text-blue-500 dark:text-blue-400 shrink-0" />
          )}
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
                activePath={activePath}
                path={`${path}${child.name}`}
              />
            ))}
          </div>
        ) : null}
      </div>
    );
  }

  const isActive = activePath === path;
  const { icon: FileIcon, className: iconClassName } = getFileIconMeta(entry.name);

  return (
    <button
      type="button"
      onClick={() => onSelectFile(path)}
      className={cn(
        "flex items-center gap-1.5 w-full px-2 py-1.5 rounded-md text-left transition-colors",
        isActive ? "bg-primary/10 text-primary" : "hover:bg-muted/60",
      )}
      style={{ paddingLeft: `${depth * 14 + 26}px` }}
    >
      <FileIcon size={13} className={cn("shrink-0", isActive ? "text-primary" : iconClassName)} />
      <span className="flex-1 text-[12.8px] truncate font-mono">{entry.name}</span>
      <span className="text-[11px] text-muted-foreground shrink-0">{formatBytes(entry.sizeBytes)}</span>
    </button>
  );
}

export function FilesPanel({ instanceId }: { instanceId: string }) {
  const { resolvedTheme } = useTheme();
  const [downloadPath, setDownloadPath] = useState("");
  const [uploadPath, setUploadPath] = useState("");
  const [isUploading, setIsUploading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [tabs, setTabs] = useState<OpenTab[]>([]);
  const [activePath, setActivePath] = useState<string | null>(null);

  const tree = useMemo(() => generateMockFileTree(instanceId), [instanceId]);
  const activeTab = tabs.find((t) => t.path === activePath) ?? null;
  const isDirty = !!activeTab && activeTab.draft !== activeTab.original;

  function openFile(path: string) {
    setDownloadPath(path);
    setActivePath(path);
    setTabs((prev) => {
      if (prev.some((t) => t.path === path)) return prev;
      const content = generateMockFileContent(path);
      return [...prev, { path, original: content ?? "", draft: content ?? "" }];
    });
  }

  function closeTab(path: string, e?: React.MouseEvent) {
    e?.stopPropagation();
    const tab = tabs.find((t) => t.path === path);
    if (tab && tab.draft !== tab.original) {
      const ok = window.confirm(`Discard unsaved changes to "${path}"?`);
      if (!ok) return;
    }
    setTabs((prev) => {
      const next = prev.filter((t) => t.path !== path);
      if (activePath === path) setActivePath(next.length ? next[next.length - 1].path : null);
      return next;
    });
  }

  function updateDraft(path: string, value: string) {
    setTabs((prev) => prev.map((t) => (t.path === path ? { ...t, draft: value } : t)));
  }

  function saveActiveTab() {
    if (!activeTab) return;
    setTabs((prev) => prev.map((t) => (t.path === activeTab.path ? { ...t, original: t.draft } : t)));
    toast.success(`Saved "${activeTab.path}" (mocked — not written to disk yet).`);
  }

  function discardActiveTab() {
    if (!activeTab) return;
    setTabs((prev) => prev.map((t) => (t.path === activeTab.path ? { ...t, draft: t.original } : t)));
  }

  function handleDownload() {
    if (!downloadPath.trim()) {
      toast.error("Enter the file's path, relative to the instance's work directory.");
      return;
    }
    const url = `/api/files/download?instanceId=${instanceId}&path=${encodeURIComponent(downloadPath)}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = url;
    downloadLink.download = "";
    downloadLink.click();
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

  const activeContent = activeTab ? generateMockFileContent(activeTab.path) : undefined;
  const isBinary = activeTab && activeContent === null;

  return (
    <div className="flex flex-col gap-6">
      <div className="grid grid-cols-1 lg:grid-cols-[240px_minmax(0,1fr)] gap-5">
        <div className="flex flex-col gap-2 rounded-lg border border-border overflow-hidden">
          <div className="flex items-center justify-between px-4 py-3 border-b border-border bg-muted/40">
            <span className="text-[12.5px] font-medium">Directory</span>
            <MockBadge
              title="The backend has no directory-listing or file-content endpoint yet — this tree and the editor contents are generated locally to preview the UX. See docs/BACKEND_REQUIREMENTS.md (GET /api/files/list, GET/PUT /api/files/content)."
            />
          </div>
          <div className="flex flex-col py-1.5 max-h-[420px] overflow-auto">
            {tree.children?.map((entry) => (
              <FileNode
                key={entry.name}
                entry={entry}
                depth={0}
                activePath={activePath}
                onSelectFile={openFile}
                path={entry.name}
              />
            ))}
          </div>
        </div>

        <div className="flex flex-col rounded-lg border border-border overflow-hidden bg-card">
          {tabs.length > 0 ? (
            <div className="flex items-center overflow-x-auto border-b border-border bg-muted/40">
              {tabs.map((tab) => {
                const dirty = tab.draft !== tab.original;
                const active = tab.path === activePath;
                return (
                  <button
                    key={tab.path}
                    type="button"
                    onClick={() => {
                      setActivePath(tab.path);
                      setDownloadPath(tab.path);
                    }}
                    className={cn(
                      "group flex items-center gap-2 px-3 py-2 text-[12px] border-r border-border shrink-0 transition-colors",
                      active ? "bg-card text-foreground" : "text-muted-foreground hover:bg-muted/70",
                    )}
                  >
                    <span className="font-mono truncate max-w-[140px]">{tab.path.split("/").pop()}</span>
                    {dirty ? <Circle size={7} className="fill-primary text-primary shrink-0" /> : null}
                    <span
                      role="button"
                      tabIndex={-1}
                      onClick={(e) => closeTab(tab.path, e)}
                      className={cn(
                        "shrink-0 rounded-sm p-0.5 hover:bg-muted transition-opacity",
                        !dirty && "opacity-0 group-hover:opacity-100",
                      )}
                    >
                      <X size={11} />
                    </span>
                  </button>
                );
              })}
            </div>
          ) : null}

          {activeTab ? (
            <>
              <div className="flex items-center justify-between px-3 py-2 border-b border-border">
                <span className="text-[11.5px] font-mono text-muted-foreground truncate">{activeTab.path}</span>
                <div className="flex items-center gap-2">
                  <Button variant="ghost" size="sm" onClick={discardActiveTab} disabled={!isDirty}>
                    <RotateCcw />
                    Discard
                  </Button>
                  <Button size="sm" onClick={saveActiveTab} disabled={!isDirty || !!isBinary}>
                    <Save />
                    Save
                  </Button>
                </div>
              </div>

              {isBinary ? (
                <div className="flex items-center justify-center py-16">
                  <EmptyState
                    icon={FileWarning}
                    title="Can't preview this file"
                    description="Binary files aren't rendered in the editor. Download it instead to inspect it locally."
                  />
                </div>
              ) : (
                <CodeMirror
                  value={activeTab.draft}
                  height="360px"
                  theme={resolvedTheme === "dark" ? oneDark : "light"}
                  extensions={[languageExtension(activeTab.path)].filter(Boolean) as never[]}
                  onChange={(value) => updateDraft(activeTab.path, value)}
                  className="text-[12.5px] [&_.cm-editor]:h-full"
                />
              )}
            </>
          ) : (
            <div className="flex items-center justify-center py-20">
              <EmptyState
                icon={File}
                title="No file open"
                description="Select a file from the directory tree to preview and edit it."
              />
            </div>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-5">
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

      <p className="text-[12px] text-muted-foreground">
        Uploads/downloads are real backend calls, one file at a time, by exact path relative
        to the instance&apos;s work directory. The editor above previews mocked content — see the
        badge on the directory panel. A Remote node caps transfers at 10 MB; a Local node has
        no cap.
      </p>
    </div>
  );
}

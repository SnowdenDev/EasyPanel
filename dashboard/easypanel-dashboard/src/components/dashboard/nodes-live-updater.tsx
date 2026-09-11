"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

/** A plain SSE connection to our own server (app/api/nodes-stream) — the real SignalR
 * connection to the backend's DashboardHub lives there, on the internal Docker network.
 * The browser just gets told "something changed" and re-fetches via router.refresh(). */
export function NodesLiveUpdater() {
  const router = useRouter();

  useEffect(() => {
    const source = new EventSource("/api/nodes-stream");
    source.addEventListener("node-connectivity-changed", () => router.refresh());
    return () => source.close();
  }, [router]);

  return null;
}

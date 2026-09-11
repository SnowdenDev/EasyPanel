import { NextRequest } from "next/server";
import { getSessionToken } from "@/lib/session";
import { createBackendConnection } from "@/lib/signalr/server-connection";

// Same bridging pattern as console-stream, for the Nodes page's live online/offline
// updates (DaemonControlHub broadcasts NodeConnectivityChanged to every connected
// DashboardHub client) — the browser gets a plain SSE ping, never the SignalR connection
// itself.
export async function GET(request: NextRequest) {
  const token = await getSessionToken();
  if (!token) {
    return new Response("Not authenticated.", { status: 401 });
  }

  const connection = createBackendConnection(token);
  let closed = false;

  const stream = new ReadableStream<Uint8Array>({
    async start(controller) {
      const encoder = new TextEncoder();
      const send = (event: string, data: unknown) => {
        if (closed) return;
        controller.enqueue(encoder.encode(`event: ${event}\ndata: ${JSON.stringify(data)}\n\n`));
      };

      connection.on("NodeConnectivityChanged", (nodeId: string, isOnline: boolean) => {
        send("node-connectivity-changed", { nodeId, isOnline });
      });

      connection.onclose(() => {
        if (!closed) {
          closed = true;
          controller.close();
        }
      });

      try {
        await connection.start();
      } catch {
        if (!closed) {
          closed = true;
          controller.close();
        }
      }
    },
    async cancel() {
      closed = true;
      await connection.stop();
    },
  });

  request.signal.addEventListener("abort", () => {
    closed = true;
    connection.stop().catch(() => {});
  });

  return new Response(stream, {
    headers: {
      "Content-Type": "text/event-stream",
      "Cache-Control": "no-cache, no-transform",
      Connection: "keep-alive",
    },
  });
}

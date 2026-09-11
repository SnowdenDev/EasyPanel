import { NextRequest } from "next/server";
import { getSessionToken } from "@/lib/session";
import { createBackendConnection } from "@/lib/signalr/server-connection";
import { registerConsoleConnection, removeConsoleConnection } from "@/lib/signalr/connection-registry";

function sseLine(event: string, data: unknown): string {
  return `event: ${event}\ndata: ${JSON.stringify(data)}\n\n`;
}

// Bridges the backend's DashboardHub to the browser as Server-Sent Events. The browser
// never opens a SignalR connection of its own — this server holds the one real connection
// to the backend (over the internal Docker network) for as long as this stream is open,
// and just forwards what it hears. See lib/signalr/server-connection.ts for why.
export async function GET(request: NextRequest, { params }: { params: Promise<{ instanceId: string }> }) {
  const { instanceId } = await params;
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
        controller.enqueue(encoder.encode(sseLine(event, data)));
      };

      connection.on("ConsoleOutputReceived", (lineInstanceId: string, streamKind: string, text: string, timestampUtc: string) => {
        if (lineInstanceId !== instanceId) return;
        send("console-output", { streamKind, text, timestampUtc });
      });

      connection.on("InstanceStatusChanged", (changedInstanceId: string, newStatus: string) => {
        if (changedInstanceId !== instanceId) return;
        send("status-changed", { status: newStatus });
      });

      connection.onreconnected(() => send("connection-state", { connected: true }));
      connection.onreconnecting(() => send("connection-state", { connected: false }));
      connection.onclose(() => {
        send("connection-state", { connected: false });
        if (!closed) {
          closed = true;
          controller.close();
        }
      });

      try {
        await connection.start();
        await connection.invoke("SubscribeToInstanceConsole", instanceId);
        registerConsoleConnection(token, instanceId, connection);
        send("connection-state", { connected: true });
      } catch {
        send("connection-state", { connected: false });
        if (!closed) {
          closed = true;
          controller.close();
        }
      }
    },
    async cancel() {
      closed = true;
      removeConsoleConnection(token, instanceId);
      try {
        await connection.invoke("UnsubscribeFromInstanceConsole", instanceId);
      } catch {
        // Connection may already be gone — nothing to clean up on the backend side then.
      }
      await connection.stop();
    },
  });

  request.signal.addEventListener("abort", () => {
    closed = true;
    removeConsoleConnection(token, instanceId);
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

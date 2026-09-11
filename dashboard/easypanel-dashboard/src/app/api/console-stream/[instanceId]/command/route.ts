import { NextRequest, NextResponse } from "next/server";
import { getSessionToken } from "@/lib/session";
import { createBackendConnection } from "@/lib/signalr/server-connection";
import { getConsoleConnection } from "@/lib/signalr/connection-registry";

export async function POST(request: NextRequest, { params }: { params: Promise<{ instanceId: string }> }) {
  const { instanceId } = await params;
  const token = await getSessionToken();
  if (!token) {
    return NextResponse.json({ message: "Not authenticated." }, { status: 401 });
  }

  const { command } = (await request.json()) as { command?: string };
  if (typeof command !== "string" || command.length === 0) {
    return NextResponse.json({ message: "command is required." }, { status: 400 });
  }

  // Reuse the same connection this tab's open console stream is already holding — the
  // common case. Falling back to a one-off connection only covers the edge case of a
  // command arriving before that stream finished subscribing.
  const existing = getConsoleConnection(token, instanceId);
  if (existing) {
    try {
      await existing.invoke("SendConsoleCommand", instanceId, command);
      return NextResponse.json({ ok: true });
    } catch (error) {
      return NextResponse.json({ message: error instanceof Error ? error.message : "Failed to send command." }, { status: 500 });
    }
  }

  const connection = createBackendConnection(token);
  try {
    await connection.start();
    await connection.invoke("SendConsoleCommand", instanceId, command);
    return NextResponse.json({ ok: true });
  } catch (error) {
    return NextResponse.json({ message: error instanceof Error ? error.message : "Failed to send command." }, { status: 500 });
  } finally {
    await connection.stop();
  }
}

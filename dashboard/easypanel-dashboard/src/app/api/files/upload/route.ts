import { NextRequest, NextResponse } from "next/server";
import { BACKEND_BASE_URL } from "@/lib/config";
import { getSessionToken } from "@/lib/session";

export async function PUT(request: NextRequest) {
  const token = await getSessionToken();
  if (!token) {
    return NextResponse.json({ message: "Not authenticated." }, { status: 401 });
  }

  const instanceId = request.nextUrl.searchParams.get("instanceId");
  const path = request.nextUrl.searchParams.get("path");
  if (!instanceId || !path) {
    return NextResponse.json({ message: "instanceId and path are required." }, { status: 400 });
  }

  const backendResponse = await fetch(
    `${BACKEND_BASE_URL}/api/instances/${instanceId}/files/upload?path=${encodeURIComponent(path)}`,
    {
      method: "PUT",
      headers: { Authorization: `Bearer ${token}` },
      body: request.body,
      // @ts-expect-error -- Node's fetch requires this when streaming a request body.
      duplex: "half",
    },
  );

  const message = backendResponse.ok ? null : await backendResponse.text().catch(() => backendResponse.statusText);
  return NextResponse.json(message ? { message } : { ok: true }, { status: backendResponse.status });
}

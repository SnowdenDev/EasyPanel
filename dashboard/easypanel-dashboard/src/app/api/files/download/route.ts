import { NextRequest, NextResponse } from "next/server";
import { BACKEND_BASE_URL } from "@/lib/config";
import { getSessionToken } from "@/lib/session";

// A real streaming proxy, not a redirect — the backend's file bytes never touch
// client-visible storage, and the Authorization header (from our httpOnly cookie) never
// reaches the browser. The browser just does <a href="/api/files/download?..."> and gets
// a normal file download.
export async function GET(request: NextRequest) {
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
    `${BACKEND_BASE_URL}/api/instances/${instanceId}/files/download?path=${encodeURIComponent(path)}`,
    { headers: { Authorization: `Bearer ${token}` } },
  );

  if (!backendResponse.ok || !backendResponse.body) {
    const message = await backendResponse.text().catch(() => backendResponse.statusText);
    return NextResponse.json({ message }, { status: backendResponse.status });
  }

  return new NextResponse(backendResponse.body, {
    status: 200,
    headers: {
      "Content-Type": backendResponse.headers.get("Content-Type") ?? "application/octet-stream",
      "Content-Disposition": backendResponse.headers.get("Content-Disposition") ?? "attachment",
    },
  });
}

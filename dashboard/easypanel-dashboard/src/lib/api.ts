import "server-only";
import { BACKEND_BASE_URL } from "@/lib/config";
import { getSessionToken } from "@/lib/session";

export class ApiRequestError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
  }
}

/** Server-side fetch helper: attaches the caller's bearer token and talks straight to the
 * backend. Only usable from Server Components/Actions/Route Handlers — never shipped to
 * the browser (enforced by the "server-only" import above). */
export async function backendFetch<T>(
  path: string,
  init: RequestInit & { skipAuth?: boolean } = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  headers.set("Content-Type", "application/json");

  if (!init.skipAuth) {
    const token = await getSessionToken();
    if (!token) {
      throw new ApiRequestError("Not authenticated.", 401);
    }
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${BACKEND_BASE_URL}${path}`, {
    ...init,
    headers,
    cache: "no-store",
  });

  if (!response.ok) {
    let message = response.statusText;
    try {
      const body = (await response.json()) as { message?: string };
      message = body.message ?? message;
    } catch {
      // Body wasn't JSON (e.g. a plain 403/409 with no payload) — statusText is fine.
    }
    throw new ApiRequestError(message, response.status);
  }

  if (response.status === 204 || response.status === 202) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

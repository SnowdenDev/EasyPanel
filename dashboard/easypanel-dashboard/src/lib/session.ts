import "server-only";
import { cookies } from "next/headers";
import { SESSION_COOKIE_NAME } from "@/lib/config";
import type { SessionUser, UserRole } from "@/lib/types";

interface JwtPayload {
  sub: string;
  email: string;
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": UserRole;
  exp: number;
}

interface SessionCookieValue {
  token: string;
  displayName: string;
}

// The backend issues a plain HS256 JWT with only sub/email/role claims (displayName comes
// separately, from the login response body, and is carried in our own session cookie
// alongside the token — see actions/auth.ts). This decodes the JWT without verifying its
// signature: that's fine here because the token only ever reaches this server through our
// own httpOnly cookie, and every real API call re-sends it to the backend, which does
// verify the signature. This decode is only used to know who's logged in for rendering,
// never to authorize anything by itself.
function decodeJwtPayload(token: string): JwtPayload {
  const payloadSegment = token.split(".")[1];
  const normalized = payloadSegment.replace(/-/g, "+").replace(/_/g, "/");
  const json = Buffer.from(normalized, "base64").toString("utf8");
  return JSON.parse(json) as JwtPayload;
}

export async function getSessionToken(): Promise<string | null> {
  const store = await cookies();
  const raw = store.get(SESSION_COOKIE_NAME)?.value;
  if (!raw) {
    return null;
  }

  try {
    const parsed = JSON.parse(raw) as SessionCookieValue;
    return parsed.token;
  } catch {
    return null;
  }
}

export async function getServerSession(): Promise<{ token: string; user: SessionUser } | null> {
  const store = await cookies();
  const raw = store.get(SESSION_COOKIE_NAME)?.value;
  if (!raw) {
    return null;
  }

  try {
    const { token, displayName } = JSON.parse(raw) as SessionCookieValue;
    const payload = decodeJwtPayload(token);
    if (payload.exp * 1000 < Date.now()) {
      return null;
    }

    return {
      token,
      user: {
        userId: payload.sub,
        email: payload.email,
        displayName,
        role: payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"],
      },
    };
  } catch {
    return null;
  }
}

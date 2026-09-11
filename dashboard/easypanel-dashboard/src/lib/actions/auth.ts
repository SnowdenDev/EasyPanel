"use server";

import { cookies, headers } from "next/headers";
import { redirect } from "next/navigation";
import { BACKEND_BASE_URL, SESSION_COOKIE_NAME } from "@/lib/config";
import type { LoginResponse } from "@/lib/types";

export interface LoginActionResult {
  error?: string;
}

export async function loginAction(_prev: LoginActionResult, formData: FormData): Promise<LoginActionResult> {
  const email = formData.get("email");
  const password = formData.get("password");

  if (typeof email !== "string" || typeof password !== "string" || email.length === 0 || password.length === 0) {
    return { error: "Email and password are required." };
  }

  const response = await fetch(`${BACKEND_BASE_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
    cache: "no-store",
  });

  if (!response.ok) {
    return { error: response.status === 401 ? "Incorrect email or password." : "Login failed. Try again." };
  }

  const body = (await response.json()) as LoginResponse;
  const expires = new Date(body.expiresAtUtc);

  // Whether this cookie can be marked Secure depends on what the BROWSER's connection
  // actually is, not on NODE_ENV=production — those are unrelated. Behind a real reverse
  // proxy terminating TLS, the browser's connection is https even though this app itself
  // only ever sees plain HTTP from the proxy, and a well-behaved proxy says so via
  // X-Forwarded-Proto. Trusting NODE_ENV instead would mark the cookie Secure any time
  // this app is deployed at all, silently breaking login the moment someone reaches it
  // over plain HTTP directly (no proxy in front yet, a bare internal/LAN deployment) — a
  // Secure cookie set over a non-HTTPS connection is dropped by the browser outright, with
  // no error surfaced anywhere.
  const forwardedProto = (await headers()).get("x-forwarded-proto");

  const store = await cookies();
  store.set({
    name: SESSION_COOKIE_NAME,
    value: JSON.stringify({ token: body.accessToken, displayName: body.displayName }),
    httpOnly: true,
    secure: forwardedProto === "https",
    sameSite: "lax",
    path: "/",
    expires,
  });

  redirect("/instances");
}

export async function logoutAction(): Promise<void> {
  const store = await cookies();
  store.delete(SESSION_COOKIE_NAME);
  redirect("/login");
}

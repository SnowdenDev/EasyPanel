"use server";

import { revalidatePath } from "next/cache";
import { ApiRequestError, backendFetch } from "@/lib/api";
import type { ServerPermissions, UserRole } from "@/lib/types";

export interface StaffActionResult {
  error?: string;
}

export async function createUserAction(
  email: string,
  displayName: string,
  role: UserRole,
  password: string,
): Promise<StaffActionResult & { userId?: string }> {
  try {
    const result = await backendFetch<{ userId: string }>("/api/users", {
      method: "POST",
      body: JSON.stringify({ email, displayName, role, password }),
    });
    revalidatePath("/staff");
    return { userId: result.userId };
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to create user." };
  }
}

export async function assignServerPermissionsAction(
  targetUserId: string,
  instanceId: string,
  permissions: ServerPermissions,
): Promise<StaffActionResult> {
  try {
    await backendFetch(`/api/staff/${targetUserId}/permissions/${instanceId}`, {
      method: "PUT",
      body: JSON.stringify({
        canViewConsole: permissions.canViewConsole,
        canSendConsoleInput: permissions.canSendConsoleInput,
        canControlPower: permissions.canControlPower,
        canAccessFileManager: permissions.canAccessFileManager,
        canEditSettings: permissions.canEditSettings,
      }),
    });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to update permissions." };
  }

  revalidatePath("/staff");
  return {};
}

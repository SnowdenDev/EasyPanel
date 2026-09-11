"use server";

import { revalidatePath } from "next/cache";
import { ApiRequestError, backendFetch } from "@/lib/api";
import type { NodeConnectivityMode, RegisterNodeResult } from "@/lib/types";

export interface RegisterNodeActionResult {
  result?: RegisterNodeResult;
  error?: string;
}

export async function registerNodeAction(
  displayName: string,
  connectivityMode: NodeConnectivityMode,
): Promise<RegisterNodeActionResult> {
  try {
    const result = await backendFetch<RegisterNodeResult>("/api/nodes", {
      method: "POST",
      body: JSON.stringify({ displayName, connectivityMode }),
    });
    revalidatePath("/nodes");
    return { result };
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to register node." };
  }
}

export interface UpdateNodeActionResult {
  error?: string;
}

export async function updateNodeAction(
  nodeId: string,
  displayName: string,
  connectivityMode: NodeConnectivityMode,
): Promise<UpdateNodeActionResult> {
  try {
    await backendFetch(`/api/nodes/${nodeId}`, {
      method: "PUT",
      body: JSON.stringify({ displayName, connectivityMode }),
    });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to update node." };
  }

  revalidatePath("/nodes");
  revalidatePath(`/nodes/${nodeId}`);
  return {};
}

export async function deleteNodeAction(nodeId: string): Promise<UpdateNodeActionResult> {
  try {
    await backendFetch(`/api/nodes/${nodeId}`, { method: "DELETE" });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to delete node." };
  }

  revalidatePath("/nodes");
  revalidatePath("/instances");
  return {};
}

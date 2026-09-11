"use server";

import { revalidatePath } from "next/cache";
import { ApiRequestError, backendFetch } from "@/lib/api";

export interface InstanceActionResult {
  error?: string;
}

export async function startInstanceAction(instanceId: string): Promise<InstanceActionResult> {
  try {
    await backendFetch(`/api/instances/${instanceId}/start`, { method: "POST" });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to start instance." };
  }

  revalidatePath("/instances");
  revalidatePath(`/instances/${instanceId}/console`);
  return {};
}

export async function stopInstanceAction(instanceId: string, force: boolean): Promise<InstanceActionResult> {
  try {
    await backendFetch(`/api/instances/${instanceId}/stop?force=${force}`, { method: "POST" });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to stop instance." };
  }

  revalidatePath("/instances");
  revalidatePath(`/instances/${instanceId}/console`);
  return {};
}

export async function restartInstanceAction(instanceId: string): Promise<InstanceActionResult> {
  try {
    await backendFetch(`/api/instances/${instanceId}/restart`, { method: "POST" });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to restart instance." };
  }

  revalidatePath("/instances");
  revalidatePath(`/instances/${instanceId}/console`);
  return {};
}

export interface CreateInstanceInput {
  nodeId: string;
  displayName: string;
  workDirectory: string;
  executableRelativePath: string;
  expectedExecutableSha256: string;
  launchArguments?: string;
  cpuLimitPercent?: number;
  memoryLimitMegabytes?: number;
}

export async function createInstanceAction(input: CreateInstanceInput): Promise<InstanceActionResult> {
  try {
    await backendFetch("/api/instances", {
      method: "POST",
      body: JSON.stringify({
        nodeId: input.nodeId,
        displayName: input.displayName,
        workDirectory: input.workDirectory,
        executableRelativePath: input.executableRelativePath,
        expectedExecutableSha256: input.expectedExecutableSha256,
        launchArguments: input.launchArguments || null,
        environmentVariables: null,
        cpuLimitPercent: input.cpuLimitPercent ?? null,
        memoryLimitMegabytes: input.memoryLimitMegabytes ?? null,
      }),
    });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to create instance." };
  }

  revalidatePath("/instances");
  return {};
}

export interface UpdateInstanceInput {
  displayName: string;
  workDirectory: string;
  executableRelativePath: string;
  expectedExecutableSha256: string;
  launchArguments?: string;
  cpuLimitPercent?: number;
  memoryLimitMegabytes?: number;
}

export async function updateInstanceAction(instanceId: string, input: UpdateInstanceInput): Promise<InstanceActionResult> {
  try {
    await backendFetch(`/api/instances/${instanceId}`, {
      method: "PUT",
      body: JSON.stringify({
        displayName: input.displayName,
        workDirectory: input.workDirectory,
        executableRelativePath: input.executableRelativePath,
        expectedExecutableSha256: input.expectedExecutableSha256,
        launchArguments: input.launchArguments || null,
        environmentVariables: null,
        cpuLimitPercent: input.cpuLimitPercent ?? null,
        memoryLimitMegabytes: input.memoryLimitMegabytes ?? null,
      }),
    });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to update instance." };
  }

  revalidatePath("/instances");
  revalidatePath(`/instances/${instanceId}/console`);
  return {};
}

export async function deleteInstanceAction(instanceId: string): Promise<InstanceActionResult> {
  try {
    await backendFetch(`/api/instances/${instanceId}`, { method: "DELETE" });
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to delete instance." };
  }

  revalidatePath("/instances");
  revalidatePath("/nodes");
  return {};
}

export async function computeExecutableHashAction(
  nodeId: string,
  workDirectory: string,
  executableRelativePath: string,
): Promise<{ sha256?: string; error?: string }> {
  try {
    const result = await backendFetch<{ sha256Hex: string }>(`/api/nodes/${nodeId}/compute-hash`, {
      method: "POST",
      body: JSON.stringify({ workDirectory, executableRelativePath }),
    });
    return { sha256: result.sha256Hex };
  } catch (error) {
    return { error: error instanceof ApiRequestError ? error.message : "Failed to compute hash." };
  }
}

import "server-only";
import type { HubConnection } from "@microsoft/signalr";

/** This Docker image runs as a persistent Node process (not a serverless function), so a
 * plain module-level Map survives across requests within the same process — good enough to
 * let the console-stream GET (which opens and holds a SignalR connection for as long as a
 * viewer's tab is open) and the command POST (sent from that same tab) share one connection
 * instead of each command opening a fresh one. Keyed by session token + instance, since two
 * different users (or the same user in two tabs) watching the same instance each get their
 * own upstream connection — simpler and more isolated than trying to fan one shared
 * connection out to multiple SSE streams. */
const activeConsoleConnections = new Map<string, HubConnection>();

function key(token: string, instanceId: string): string {
  return `${token}:${instanceId}`;
}

export function registerConsoleConnection(token: string, instanceId: string, connection: HubConnection): void {
  activeConsoleConnections.set(key(token, instanceId), connection);
}

export function getConsoleConnection(token: string, instanceId: string): HubConnection | undefined {
  return activeConsoleConnections.get(key(token, instanceId));
}

export function removeConsoleConnection(token: string, instanceId: string): void {
  activeConsoleConnections.delete(key(token, instanceId));
}

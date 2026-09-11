import "server-only";
import * as signalR from "@microsoft/signalr";
import { BACKEND_BASE_URL } from "@/lib/config";

/** Opens a SignalR connection to the backend's DashboardHub from the dashboard's own
 * server, over the internal network (BACKEND_BASE_URL — the Docker service name, never a
 * public address). The browser never talks to the backend directly for anything: the
 * backend's only public-facing surface is the Daemon's own hub, a completely separate
 * concern. This connection is then bridged to the browser as Server-Sent Events by the
 * Route Handlers in app/api/console-stream and app/api/nodes-stream. */
export function createBackendConnection(token: string): signalR.HubConnection {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${BACKEND_BASE_URL}/hubs/dashboard`, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build();
}

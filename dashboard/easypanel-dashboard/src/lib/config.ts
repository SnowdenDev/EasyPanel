// The one address the whole dashboard needs to know. Everything else — REST calls from
// Server Components/Actions, the SignalR connection from the browser — targets this.
export const BACKEND_BASE_URL = process.env.EASYPANEL_BACKEND_URL ?? "http://localhost:5299";

export const SESSION_COOKIE_NAME = "ep_session";

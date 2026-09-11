import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // No `output: "standalone"` — @microsoft/signalr's Node-side HTTP client (used
  // server-side now, see lib/signalr/server-connection.ts) pulls in a chain of optional
  // peer packages (ws, eventsource, tough-cookie, and whatever those themselves need)
  // via dynamic requires that standalone's dependency tracer doesn't reliably follow
  // through an externalized package — each one only surfaced as a fresh "Cannot find
  // module" at request time, one at a time, after the previous one was fixed. Running
  // the full node_modules straight off `npm ci` sidesteps that whole class of gap
  // instead of chasing every transitive optional dependency individually. The image is
  // bigger for it; that's a fine trade for not silently missing the next one.

  // Still needed regardless of standalone: Turbopack's server bundler can't statically
  // resolve @microsoft/signalr's own dynamic require() for picking a transport, and
  // throws "dynamic usage of require is not supported" the moment a Route Handler
  // constructs a HubConnection — compiles fine, only fails at request time. Excluding it
  // from bundling makes Node load it via a normal runtime require instead.
  serverExternalPackages: ["@microsoft/signalr"],
};

export default nextConfig;

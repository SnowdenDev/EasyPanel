# EasyPanel Dashboard

The server-rendered Next.js 16 interface for EasyPanel. The browser talks only to this
application; server actions and route handlers proxy authenticated REST and SignalR traffic
to the ASP.NET Core backend. JWTs remain in HTTP-only cookies and are not exposed to client
components.

## Development

Use Node.js 22 and point the server-side backend URL at a running EasyPanel backend:

```bash
cp .env.example .env.local
# Set EASYPANEL_BACKEND_URL in .env.local, then:
npm ci
npm run dev
```

Open `http://localhost:3000`. Before submitting a change, run:

```bash
npm run lint
npm run build
npm audit --omit=dev --audit-level=high
```

## Current limitations

Fleet resource history and the directory-tree/editor portion of the file panel use visibly
labelled preview data until the corresponding backend protocol is implemented. Exact-path
file upload and download are real and pass through authenticated server-side routes.

For the complete topology, Docker workflow, and security requirements, see the repository's
[deployment guide](../../docs/deployment.md).

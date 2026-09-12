# Security policy

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability. Use GitHub's
[private vulnerability reporting](https://github.com/SnowdenDev/EasyPanel/security/advisories/new)
and include the affected component, reproduction steps, impact, and any proposed fix.

Please do not include real passwords, JWTs, raw node tokens, database dumps, or private
hostnames. You can expect an acknowledgement within seven days. A coordinated disclosure
date will be agreed after the report has been reproduced and a fix is ready.

## Supported versions

EasyPanel is currently pre-1.0 and does not yet publish stable release branches. Security
fixes are applied to the latest revision of `main`. Operators should update to the latest
commit and rebuild all affected components.

## Deployment responsibilities

EasyPanel must run behind HTTPS when exposed outside a trusted network. Replace every
placeholder secret, keep the backend and Postgres off the public internet except where
daemon connectivity requires a backend endpoint, and treat raw node tokens as passwords.
See [docs/deployment.md](docs/deployment.md) for the production topology.

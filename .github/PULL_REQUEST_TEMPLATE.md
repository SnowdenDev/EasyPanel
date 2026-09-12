## What changed?

Describe the problem and the solution. Keep unrelated changes in separate pull requests.

## How was it verified?

- [ ] `dotnet test EasyPanel.slnx -c Release`
- [ ] `npm run lint` in `dashboard/easypanel-dashboard`
- [ ] `npm run build` in `dashboard/easypanel-dashboard`
- [ ] I added or updated tests for security boundaries and behavior changes
- [ ] I updated documentation when configuration or behavior changed

## Security and compatibility

- [ ] No credentials, tokens, private URLs, or personal data are included
- [ ] Shared daemon/backend contract changes compile on both sides
- [ ] Database changes include an EF Core migration

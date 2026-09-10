# Contributing to EasyPanel

Thanks for looking at this project. It's open source under AGPL-3.0 specifically so
outside contributors can read, understand, and extend it without archaeology. The
rules below exist to keep that true as the codebase grows.

## Code should read like plain language, not like a puzzle

- Use full, explicit names. `request`, not `req`. `configuration`, not `cfg`.
  `instance`, not `inst`. If you'd have to explain an abbreviation to a new
  contributor, spell it out instead.
- Prefer explicit control flow over clever one-liners. A few extra lines that
  read top-to-bottom beat a dense expression that needs re-reading.
- Comments explain **why**, never **what**. If a comment just restates the code
  in English, delete it. Write a comment only for a non-obvious constraint, a
  workaround for a specific bug, or an invariant that would surprise a reader
  (e.g. "the backend has no filesystem access to node disks — this hash is
  either admin-declared or fetched from the daemon, never computed locally").
- No premature abstraction. Three similar lines are better than a shared helper
  built for a hypothetical future case. Don't add configuration options, feature
  flags, or extension points nothing currently needs.
- No speculative error handling for scenarios that can't happen. Validate at
  real boundaries (user input, data crossing the daemon↔backend wire) — trust
  your own internal code otherwise.

## Backend: vertical slice architecture

Features live under `backend/EasyPanel.Backend/Features/<Area>/<UseCase>/`, one
folder per use case, not one folder per technical layer. A typical slice looks
like:

```
Features/Instances/CreateInstance/
├── CreateInstanceEndpoint.cs        # route mapping
├── CreateInstanceRequest.cs         # input DTO
├── CreateInstanceRequestValidator.cs # FluentValidation rules
├── CreateInstanceHandler.cs         # the actual use case
└── CreateInstanceResponse.cs        # output DTO
```

`Infrastructure/` is reserved for things that are genuinely cross-cutting and
would be absurd to duplicate per slice: the `DbContext`, the SignalR hub shells,
authentication/authorization plumbing, the audit log writer, global exception
handling. If you're tempted to add something there, ask first whether it
actually belongs in a slice instead.

There's no MediatR or other mediator library here — a slice's endpoint calls its
handler directly through DI. Don't introduce one without discussing it first;
vertical slice doesn't require it, and it's one more thing a new contributor has
to learn before they can follow the code.

## Daemon: same standard, plus Windows-native constraints

The daemon is compiled with Native AOT. Any new P/Invoke call must use
`[LibraryImport]` (source-generated marshalling), not classic `[DllImport]` —
AOT removes the JIT-time marshalling stubs `DllImport`'s default marshalling
relies on. Wrap any new native handle in a `SafeHandle` subclass, not a raw
`IntPtr`/`nint`.

## Tests

Tests live in the sibling `.Tests` project, mirroring the folder path of the
code they cover (e.g. `Features/Instances/CreateInstance/CreateInstanceHandler.cs`
→ `EasyPanel.Backend.Tests/Features/Instances/CreateInstance/CreateInstanceHandlerTests.cs`).
Security-boundary logic (path traversal guards, hash verification, permission
checks) needs real automated tests, not just manual verification — these are the
places a regression is both easy to introduce and expensive to miss.

## License

By contributing, you agree your contribution is licensed under AGPL-3.0, same as
the rest of the project. See [LICENSE](LICENSE).

// A real throwaway "dedicated server" that deliberately crashes shortly after starting —
// exercises the daemon's crash-detection and auto-restart/backoff path for real, not via
// a mock. See docs/architecture.md's end-to-end verification checklist.

Console.WriteLine("CrashTestServer starting — will crash in 3 seconds.");
await Task.Delay(TimeSpan.FromSeconds(3));
Console.WriteLine("CrashTestServer crashing now.");
Environment.Exit(1);

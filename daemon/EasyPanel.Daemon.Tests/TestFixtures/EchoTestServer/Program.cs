// A real throwaway "dedicated server" for exercising the daemon's actual launch path —
// prints an incrementing counter every second (so live console streaming is visible) and
// echoes back any line typed into its stdin (so console input forwarding is visible).
// See docs/architecture.md's end-to-end verification checklist.

Console.WriteLine("EchoTestServer starting.");

_ = Task.Run(async () =>
{
    string? line;
    while ((line = await Console.In.ReadLineAsync()) is not null)
    {
        Console.WriteLine($"echo: {line}");
    }
});

var counter = 0;
while (true)
{
    Console.WriteLine($"tick {counter}");
    counter++;
    await Task.Delay(TimeSpan.FromSeconds(1));
}

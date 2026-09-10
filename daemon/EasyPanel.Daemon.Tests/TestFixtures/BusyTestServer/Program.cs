// A real throwaway "dedicated server" that deliberately burns CPU on every core and keeps
// allocating memory forever — exercises the daemon's Job Object CPU%/RAM limit enforcement
// for real (a low memory cap should get this process killed by the OS; a low CPU% cap
// should visibly throttle it in Task Manager). See docs/architecture.md.

Console.WriteLine($"BusyTestServer starting on {Environment.ProcessorCount} logical processors.");

for (var i = 0; i < Environment.ProcessorCount; i++)
{
    _ = Task.Run(() =>
    {
        while (true)
        {
            // Deliberately CPU-bound busy work — no awaits, no yields.
            double accumulator = 0;
            for (var j = 0; j < 10_000_000; j++)
            {
                accumulator += Math.Sqrt(j);
            }
        }
    });
}

var held = new System.Collections.Generic.List<byte[]>();
var megabytesAllocated = 0;

while (true)
{
    held.Add(new byte[10 * 1024 * 1024]);
    megabytesAllocated += 10;
    Console.WriteLine($"Allocated {megabytesAllocated} MB total.");
    await Task.Delay(TimeSpan.FromMilliseconds(200));
}

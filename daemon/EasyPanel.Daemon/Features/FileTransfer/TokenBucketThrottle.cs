using System.Threading;

namespace EasyPanel.Daemon.Features.FileTransfer;

/// <summary>
/// One shared bucket per node (registered as a singleton), not one per transfer — so
/// several simultaneous transfers can't collectively saturate the node's uplink. See
/// docs/architecture.md's file transfer section.
/// </summary>
internal sealed class TokenBucketThrottle
{
    private readonly double _bytesPerSecond;
    private readonly Lock _gate = new();
    private double _availableTokens;
    private DateTime _lastRefillUtc = DateTime.UtcNow;

    public TokenBucketThrottle(double bytesPerSecond)
    {
        _bytesPerSecond = bytesPerSecond;
        _availableTokens = bytesPerSecond;
    }

    public async Task ConsumeAsync(int byteCount, CancellationToken cancellationToken)
    {
        while (true)
        {
            TimeSpan waitTime;

            lock (_gate)
            {
                Refill();

                if (_availableTokens >= byteCount)
                {
                    _availableTokens -= byteCount;
                    return;
                }

                var deficit = byteCount - _availableTokens;
                waitTime = TimeSpan.FromSeconds(deficit / _bytesPerSecond);
            }

            await Task.Delay(waitTime, cancellationToken);
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsedSeconds = (now - _lastRefillUtc).TotalSeconds;
        _availableTokens = Math.Min(_bytesPerSecond, _availableTokens + (elapsedSeconds * _bytesPerSecond));
        _lastRefillUtc = now;
    }
}

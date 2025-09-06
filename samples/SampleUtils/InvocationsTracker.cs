using System.Diagnostics;

namespace SampleUtils;

public class InvocationsTracker
{
    private int _totalCounter;
    private int _sinceLastSecondCounter;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public (int Invocations, double? PerSecond) InvokeAndGetInvocations(int count = 1)
    {
        long elapsed;
        int invocationsSinceLastTimeThreshold, total;

        lock (_stopwatch)
        {
            _totalCounter += count;
            _sinceLastSecondCounter += count;

            total = _totalCounter;
            invocationsSinceLastTimeThreshold = _sinceLastSecondCounter;
            elapsed = _stopwatch.ElapsedMilliseconds;

            if (elapsed >= 1000)
            {
                _stopwatch.Restart();
                _sinceLastSecondCounter = 0;
            }
        }
        
        return (total,
            elapsed is >= 1000 and <= 2000
            ? Math.Round(invocationsSinceLastTimeThreshold * 1000f / elapsed, 2)
            : null);
    }
}
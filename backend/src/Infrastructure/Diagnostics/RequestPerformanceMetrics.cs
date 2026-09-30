namespace Infrastructure.Diagnostics;

public sealed class RequestPerformanceMetrics
{
    private int _queryCount;

    public int QueryCount => _queryCount;

    public void IncrementQueryCount() => Interlocked.Increment(ref _queryCount);
}

using System.Diagnostics.Metrics;

namespace ProjectAscension.Api.Services;

/// <summary>
/// Metrics for AI skill composition (System.Diagnostics.Metrics) — completions,
/// deferrals, attempts-per-pass, and duration. Published under the meter
/// "ProjectAscension.SkillForge" for any OpenTelemetry/Prometheus exporter to scrape.
/// </summary>
public sealed class CompositionMetrics : IDisposable
{
    public const string MeterName = "ProjectAscension.SkillForge";

    private readonly Meter _meter;
    private readonly Counter<long> _completed;
    private readonly Counter<long> _deferred;
    private readonly Histogram<int> _attempts;
    private readonly Histogram<double> _durationMs;

    public CompositionMetrics()
    {
        _meter = new Meter(MeterName);
        _completed = _meter.CreateCounter<long>("discovery.composition.completed", "{skill}", "Skills composed and frozen to Ready.");
        _deferred = _meter.CreateCounter<long>("discovery.composition.deferred", "{skill}", "Compositions deferred (left Pending to retry).");
        _attempts = _meter.CreateHistogram<int>("discovery.composition.attempts", "{attempt}", "Composer attempts per pass.");
        _durationMs = _meter.CreateHistogram<double>("discovery.composition.duration", "ms", "Time to compose one skill.");
    }

    public void Completed(int attempts, double milliseconds)
    {
        _completed.Add(1);
        _attempts.Record(attempts);
        _durationMs.Record(milliseconds);
    }

    public void Deferred(int attempts, double milliseconds)
    {
        _deferred.Add(1);
        _attempts.Record(attempts);
        _durationMs.Record(milliseconds);
    }

    public void Dispose() => _meter.Dispose();
}

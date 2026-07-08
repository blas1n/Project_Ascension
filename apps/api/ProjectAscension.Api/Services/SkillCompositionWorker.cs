namespace ProjectAscension.Api.Services;

/// <summary>
/// Drives async content composition: pulls Pending discovery skills and composes
/// them on an interval, so the trigger (fact) stays instant and the AI content
/// fills in behind it (ADR 0002). Resolves the scoped service per pass.
/// </summary>
public class SkillCompositionWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 10;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SkillCompositionWorker> _logger;

    public SkillCompositionWorker(IServiceScopeFactory scopeFactory, ILogger<SkillCompositionWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // One-time backfill (ADR 0007 Phase 4c-4): translate any legacy graphless discovery to a
        // graph so the runtime is single-path and PrimitivesJson can eventually retire. Idempotent
        // (only touches rows with no graph), so it's safe to run every startup.
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ISkillCompositionService>();
            await service.BackfillGraphsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
        catch (Exception ex) { _logger.LogError(ex, "Effect-graph backfill failed."); }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ISkillCompositionService>();
                await service.ComposePendingAsync(BatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Skill composition pass failed.");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}

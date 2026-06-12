using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorldCupBets.Application.Abstractions;
using WorldCupBets.Application.Features.Matches;
using WorldCupBets.Domain.Repositories;

namespace WorldCupBets.Infrastructure.ExternalFootball;

public sealed class MatchResultSyncBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    MatchResultSyncOptions options,
    ILogger<MatchResultSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("Match result sync background service is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, options.PollIntervalMinutes)));

        await RunOnceAsync(stoppingToken);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunOnceAsync(stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var result = await SyncMatchResultsHandler.Handle(
                new SyncMatchResultsCommand(),
                scope.ServiceProvider.GetRequiredService<IFootballDataProvider>(),
                scope.ServiceProvider.GetRequiredService<IOfficialMatchResultProvider>(),
                scope.ServiceProvider.GetRequiredService<IMatchRepository>(),
                scope.ServiceProvider.GetRequiredService<IMatchBetRepository>(),
                scope.ServiceProvider.GetRequiredService<ITournamentSettlementRepository>(),
                scope.ServiceProvider.GetRequiredService<IUserRepository>(),
                scope.ServiceProvider.GetRequiredService<IApplicationTransactionFactory>(),
                cancellationToken);

            if (result.CandidatesConsideredCount > 0 || result.FailedCount > 0)
            {
                logger.LogInformation(
                    "Match result sync run completed. candidates={Candidates} confirmed={Confirmed} alreadySettled={AlreadySettled} deferred={Deferred} failed={Failed}",
                    result.CandidatesConsideredCount,
                    result.ConfirmedCount,
                    result.AlreadySettledCount,
                    result.DeferredCount,
                    result.FailedCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Match result sync background run failed.");
        }
    }
}

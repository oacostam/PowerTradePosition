using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Console;

public class ScheduledExtractor(
    IPositionExtractor positionExtractor,
    IScheduleCalculator scheduleCalculator,
    ILogger<ScheduledExtractor> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            logger.LogInformation("Starting scheduled extractor");
            await ScheduleExtractionsAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in scheduled extractor");
            throw;
        }
    }

    private async Task ScheduleExtractionsAsync(CancellationToken stoppingToken)
    {
        // Run initial extraction immediately
        logger.LogInformation("Starting initial position extraction");
        await RunExtractionAsync(stoppingToken);
        logger.LogInformation("Initial extraction completed successfully");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Calculate next execution time using the schedule calculator
                var delay = scheduleCalculator.CalculateDelayUntilNextExecution();

                var nextInterval = DateTime.UtcNow.Add(delay);
                logger.LogInformation("Next extraction scheduled for: {NextExtraction} (in {Delay:F1} minutes)",
                    nextInterval.ToString("yyyy-MM-dd HH:mm:ss UTC"), delay.TotalMinutes);

                // Wait until next interval
                await Task.Delay(delay, stoppingToken);

                // Check if we're still within the execution window (tolerance of +/- 1 minute)
                if (!scheduleCalculator.IsWithinExecutionWindow())
                {
                    logger.LogWarning("Scheduled time has passed tolerance window, running extraction immediately");
                }
                await RunExtractionAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Service is stopping
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in scheduled extraction cycle");

                // Wait a bit before retrying the cycle
                if (!stoppingToken.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task RunExtractionAsync(CancellationToken stoppingToken)
    {
        const int errorDelaySeconds = 5;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var stopWatch = Stopwatch.StartNew();
                logger.LogInformation("Running position extraction");

                await positionExtractor.ExtractPositionsAsync(stoppingToken);

                logger.LogInformation("Extraction completed successfully in {Duration:F2} seconds", stopWatch.Elapsed.TotalSeconds);
                return; // Success - exit the retry loop
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Extraction failed, will retry in {errorDelaySeconds}", errorDelaySeconds);
                
                // Wait before retrying (don't overwhelm the system)
                if (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(errorDelaySeconds), stoppingToken);
                }
            }
        }
    }
}
using Microsoft.Extensions.Logging;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.Domain;

public class PositionExtractor(
    ITradeService tradeService,
    IPositionAggregator positionAggregator,
    ICsvWriter csvWriter,
    
    ApplicationConfiguration configuration,
    TimeProvider timeProvider,
    IScheduleCalculator scheduleCalculator,
    ILogger<PositionExtractor> logger)
    : IPositionExtractor
{
    public async Task ExtractPositionsAsync(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Starting position extraction");

            // Get the day-ahead date (tomorrow) in the configured timezone (Europe/Berlin)
            var dayAheadDate = scheduleCalculator.CalculateDayAheadDate();
            logger.LogInformation("Extracting positions for day-ahead date: {Date} in timezone {TimeZone}",
                dayAheadDate.ToString("yyyy-MM-dd"), configuration.TimeZoneId);

            // Retrieve trades from the service
            var trades = await tradeService.GetTradesAsync(dayAheadDate, ct);
            var powerTrades = trades as PowerTrade[] ?? trades.ToArray();
            if (powerTrades.Length == 0)
            {
                logger.LogWarning("No trades found for date: {Date}", dayAheadDate.ToString("yyyy-MM-dd"));
                return;
            }

            logger.LogInformation("Retrieved {Count} trades for date: {Date}", powerTrades.Length,
                dayAheadDate.ToString("yyyy-MM-dd"));

            // Aggregate positions by hour
            var positions = positionAggregator.AggregatePositionsByHour(powerTrades, configuration.TimeZoneId);
            var powerPositions = positions as PowerPosition[] ?? positions.ToArray();
            if (powerPositions.Length == 0)
            {
                logger.LogWarning("No positions generated for date: {Date}", dayAheadDate.ToString("yyyy-MM-dd"));
                return;
            }

            logger.LogInformation("Generated {Count} hourly positions", powerPositions.Length);

            // Write to CSV with automatic filename generation
            var extractionTime = timeProvider.GetUtcNow().DateTime;
            await csvWriter.WriteToFileAsync(powerPositions, dayAheadDate, extractionTime, configuration.OutputFolderPath, ct);

            logger.LogInformation("Position extraction completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during position extraction");
            throw;
        }
    }
}
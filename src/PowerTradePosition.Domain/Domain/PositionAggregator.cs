using Microsoft.Extensions.Logging;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.Domain;

public class PositionAggregator(ITimeGridBuilder timeGridBuilder, ILogger<PositionAggregator> logger)
    : IPositionAggregator
{
    public IEnumerable<PowerPosition> AggregatePositionsByHour(IEnumerable<PowerTrade> trades, string timeZoneId)
    {
        try
        {
            var powerTrades = trades as PowerTrade[] ?? trades.ToArray();
            if (powerTrades.Length == 0)
            {
                logger.LogWarning("No trades provided for aggregation");
                return [];
            }

            var dayAheadDate = powerTrades.First().Date;
            var timeGrid = timeGridBuilder.BuildHourlyTimeGrid(dayAheadDate, timeZoneId).ToList();
            var hourlyVolumes = InitializeHourlyVolumes(timeGrid);
            
            AggregateTradeVolumes(powerTrades, timeGrid, hourlyVolumes);
            
            var positions = CreatePowerPositions(hourlyVolumes);
            
            logger.LogInformation("Successfully aggregated {Count} hourly positions for date {Date}",
                positions.Count, dayAheadDate.ToString("yyyy-MM-dd"));

            return positions;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error aggregating positions by hour");
            throw;
        }
    }

    private static Dictionary<DateTime, double> InitializeHourlyVolumes(IList<DateTime> timeGrid)
    {
        var hourlyVolumes = new Dictionary<DateTime, double>();
        foreach (var time in timeGrid)
        {
            hourlyVolumes[time] = 0.0;
        }
        return hourlyVolumes;
    }

    private void AggregateTradeVolumes(IEnumerable<PowerTrade> trades, IList<DateTime> timeGrid, Dictionary<DateTime, double> hourlyVolumes)
    {
        foreach (var trade in trades)
        {
            foreach (var period in trade.Periods)
            {
                if (IsValidPeriod(period.Period, timeGrid.Count))
                {
                    var hourTime = GetHourTime(period.Period, timeGrid);
                    hourlyVolumes[hourTime] += period.Volume;
                    var hourIndex = period.Period - 1;
                    logger.LogDebug(
                        "Mapping period {Period} (index {Index}) to time {Time} with volume {Volume}. Total for this time: {Total}",
                        period.Period, hourIndex, hourTime.ToString("yyyy-MM-ddTHH:mm:ssZ"), period.Volume, hourlyVolumes[hourTime]);
                }
            }
        }
    }

    private static bool IsValidPeriod(int period, int timeGridCount)
    {
        return period is >= 1 and <= 24 && period - 1 < timeGridCount;
    }

    private static DateTime GetHourTime(int period, IList<DateTime> timeGrid)
    {
        var hourIndex = period - 1;
        return timeGrid[hourIndex];
    }

    private static List<PowerPosition> CreatePowerPositions(Dictionary<DateTime, double> hourlyVolumes)
    {
        return hourlyVolumes
            .Select(kvp => new PowerPosition(kvp.Key, kvp.Value))
            .OrderBy(p => p.DateTime)
            .ToList();
    }
}
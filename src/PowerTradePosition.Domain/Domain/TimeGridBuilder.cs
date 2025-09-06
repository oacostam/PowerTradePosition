using Microsoft.Extensions.Logging;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.Domain;

public class TimeGridBuilder : ITimeGridBuilder
{
    private readonly ILogger<TimeGridBuilder> _logger;

    public TimeGridBuilder(ILogger<TimeGridBuilder> logger)
    {
        _logger = logger;
    }

    public IEnumerable<DateTime> BuildHourlyTimeGrid(DateTime date, string timeZoneId)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localDate = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);

            // Check if this date has DST transitions
            return HasDstTransition(localDate, timeZone) ? BuildTimeGridWithDstTransitions(localDate, timeZone) : BuildNormalTimeGrid(localDate, timeZone);
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogError(ex, "Time zone {TimeZone} not found", timeZoneId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building time grid for date {Date} with timezone {TimeZone}",
                date.ToString("yyyy-MM-dd"), timeZoneId);
            throw;
        }
    }

    private static bool HasDstTransition(DateTime localDate, TimeZoneInfo timeZone)
    {
        // Check if any hour in the day has DST transitions
        for (var hour = 0; hour < 24; hour++)
        {
            var localDateTime = localDate.AddHours(hour);
            if (timeZone.IsInvalidTime(localDateTime) || timeZone.IsAmbiguousTime(localDateTime))
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerable<DateTime> BuildNormalTimeGrid(DateTime localDate, TimeZoneInfo timeZone)
    {
        var timeGrid = new List<DateTime>();
        
        for (var hour = 0; hour < 24; hour++)
        {
            var localDateTime = localDate.AddHours(hour);
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
            timeGrid.Add(utcDateTime);
        }

        _logger.LogDebug(
            "Built normal time grid for date {Date} with timezone {TimeZone}. Total entries: {Count}",
            localDate.ToString("yyyy-MM-dd"), timeZone.Id, timeGrid.Count);

        return timeGrid;
    }

    private IEnumerable<DateTime> BuildTimeGridWithDstTransitions(DateTime localDate, TimeZoneInfo timeZone)
    {
        var timeGrid = new List<DateTime>();
        
        for (var hour = 0; hour < 24; hour++)
        {
            var localDateTime = localDate.AddHours(hour);
            
            if (timeZone.IsInvalidTime(localDateTime))
            {
                // DST Spring Forward: Skip the invalid hour
                _logger.LogDebug(
                    "Skipping invalid time {LocalTime} during DST spring forward",
                    localDateTime.ToString("yyyy-MM-ddTHH:mm:ss"));
                continue;
            }
            
            var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, timeZone);
            timeGrid.Add(utcDateTime);
            
            if (timeZone.IsAmbiguousTime(localDateTime))
            {
                // DST Fall Back: Add the second occurrence (standard time)
                var secondOccurrenceUtc = GetSecondOccurrenceUtc(localDateTime, timeZone, utcDateTime);
                if (secondOccurrenceUtc.HasValue)
                {
                    timeGrid.Add(secondOccurrenceUtc.Value);
                    _logger.LogDebug(
                        "DST Fallback: Added second occurrence of hour {LocalTime} -> {UtcTime} (standard time)",
                        localDateTime.ToString("yyyy-MM-ddTHH:mm:ss"), secondOccurrenceUtc.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"));
                }
            }
        }

        // Sort the time grid to ensure proper chronological order
        timeGrid.Sort();

        _logger.LogDebug(
            "Built DST transition time grid for date {Date} with timezone {TimeZone}. First hour: {FirstHour}, Last hour: {LastHour}, Total entries: {Count}",
            localDate.ToString("yyyy-MM-dd"), timeZone.Id, 
            timeGrid.First().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            timeGrid.Last().ToString("yyyy-MM-ddTHH:mm:ssZ"), 
            timeGrid.Count);

        return timeGrid;
    }

    private static DateTime? GetSecondOccurrenceUtc(DateTime localDateTime, TimeZoneInfo timeZone, DateTime firstOccurrenceUtc)
    {
        // Get the adjustment rule for this date
        var adjustmentRule = timeZone.GetAdjustmentRules()
            .FirstOrDefault(rule => localDateTime >= rule.DateStart && localDateTime <= rule.DateEnd);
        
        if (adjustmentRule != null)
        {
            // Calculate the second occurrence by adding the daylight delta
            return firstOccurrenceUtc.Add(adjustmentRule.DaylightDelta);
        }
        
        return null;
    }
}
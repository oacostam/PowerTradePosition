using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.Domain;

/// <summary>
/// Calculates execution schedules for power position extractions
/// </summary>
public class ScheduleCalculator(ApplicationConfiguration configuration, TimeProvider timeProvider)
    : IScheduleCalculator
{
    private readonly ApplicationConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

    public DateTime CalculateNextInterval()
    {
        var currentTime = _timeProvider.GetUtcNow().DateTime;
        var intervalMinutes = _configuration.ExtractIntervalMinutes;

        // Round down to the nearest interval
        var minutesSinceMidnight = currentTime.Hour * 60 + currentTime.Minute;
        var intervalsSinceMidnight = minutesSinceMidnight / intervalMinutes;
        var nextIntervalMinutes = (intervalsSinceMidnight + 1) * intervalMinutes;

        var nextInterval = currentTime.Date.AddMinutes(nextIntervalMinutes);

        // If we're past the last interval of the day, schedule for tomorrow
        if (nextInterval <= currentTime)
            nextInterval = nextInterval.AddDays(1);

        return nextInterval;
    }

    public TimeSpan CalculateDelayUntilNextExecution()
    {
        var nextInterval = CalculateNextInterval();
        var currentTime = _timeProvider.GetUtcNow().DateTime;
        return nextInterval - currentTime;
    }

    /// <summary>
    /// Checks if the current time is within the acceptable range for execution
    /// The extract does not have to run exactly on the minute and can be within +/- 1 minute
    /// </summary>
    public bool IsWithinExecutionWindow()
    {
        var currentTime = _timeProvider.GetUtcNow().DateTime;
        var intervalMinutes = _configuration.ExtractIntervalMinutes;
        
        // Calculate the current interval
        var minutesSinceMidnight = currentTime.Hour * 60 + currentTime.Minute;
        var currentIntervalMinutes = (minutesSinceMidnight / intervalMinutes) * intervalMinutes;
        var currentInterval = currentTime.Date.AddMinutes(currentIntervalMinutes);
        
        // Check if we're within +/- 1 minute of the interval
        var tolerance = TimeSpan.FromMinutes(1);
        var timeDifference = Math.Abs((currentTime - currentInterval).TotalMinutes);
        
        return timeDifference <= tolerance.TotalMinutes;
    }

    /// <summary>
    /// Calculates the day-ahead date in the configured timezone (Europe/Berlin)
    /// This ensures the application considers the Europe/Berlin location as specified in requirements
    /// </summary>
    public DateTime CalculateDayAheadDate()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_configuration.TimeZoneId);
        var nowInTimeZone = TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow().DateTime, timeZone);
        return nowInTimeZone.Date.AddDays(1);
    }

    /// <summary>
    /// Gets the current time in the configured timezone
    /// </summary>
    public DateTime GetCurrentTimeInConfiguredTimeZone()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_configuration.TimeZoneId);
        return TimeZoneInfo.ConvertTime(_timeProvider.GetUtcNow().DateTime, timeZone);
    }
}

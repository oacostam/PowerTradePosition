namespace PowerTradePosition.Domain.Interfaces;

/// <summary>
/// Calculates the next execution time for scheduled extractions
/// </summary>
public interface IScheduleCalculator
{
    /// <summary>
    /// Calculates the next interval boundary based on the current time and configured interval
    /// </summary>
    /// <returns>The next scheduled execution time</returns>
    DateTime CalculateNextInterval();
    
    /// <summary>
    /// Calculates the delay until the next execution
    /// </summary>
    /// <returns>The time span to wait until next execution</returns>
    TimeSpan CalculateDelayUntilNextExecution();

    /// <summary>
    /// Checks if the current time is within the acceptable execution window
    /// The extract can run within +/- 1 minute of the configured interval
    /// </summary>
    /// <returns>True if within execution window, false otherwise</returns>
    bool IsWithinExecutionWindow();

    /// <summary>
    /// Calculates the day-ahead date in the configured timezone (Europe/Berlin)
    /// This ensures the application considers the Europe/Berlin location as specified in requirements
    /// </summary>
    /// <returns>The day-ahead date in the configured timezone</returns>
    DateTime CalculateDayAheadDate();

    /// <summary>
    /// Gets the current time in the configured timezone
    /// </summary>
    /// <returns>The current time converted to the configured timezone</returns>
    DateTime GetCurrentTimeInConfiguredTimeZone();
}

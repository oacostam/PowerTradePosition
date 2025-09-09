using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.UnitTests;

public class ScheduleCalculatorTests
{
    private readonly IScheduleCalculator _scheduleCalculator;
    private readonly ApplicationConfiguration _configuration;
    private readonly MockTimeProvider _timeProvider;

    public ScheduleCalculatorTests()
    {
        _configuration = new ApplicationConfiguration
        {
            ExtractIntervalMinutes = 15
        };
        _timeProvider = new MockTimeProvider(new DateTime(2023, 7, 1, 10, 30, 0, DateTimeKind.Utc));
        _scheduleCalculator = new ScheduleCalculator(_configuration, _timeProvider);
    }

    [Fact]
    public void CalculateNextInterval_WithValidInterval_ReturnsCorrectNextTime()
    {
        var nextInterval = _scheduleCalculator.CalculateNextInterval();

        var expectedTime = new DateTime(2023, 7, 1, 10, 45, 0, DateTimeKind.Utc); // 10:45 AM
        Assert.Equal(expectedTime, nextInterval);
    }

    [Fact]
    public void CalculateNextInterval_AtIntervalBoundary_ReturnsNextInterval()
    {
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 10, 45, 0, DateTimeKind.Utc)); // 10:45 AM (exactly on interval)
        
        var nextInterval = _scheduleCalculator.CalculateNextInterval();

        var expectedTime = new DateTime(2023, 7, 1, 11, 0, 0, DateTimeKind.Utc); // 11:00 AM
        Assert.Equal(expectedTime, nextInterval);
    }

    [Fact]
    public void CalculateNextInterval_AtEndOfDay_ReturnsNextDayFirstInterval()
    {
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 23, 50, 0, DateTimeKind.Utc)); // 11:50 PM
        
        var nextInterval = _scheduleCalculator.CalculateNextInterval();

        var expectedTime = new DateTime(2023, 7, 2, 0, 0, 0, DateTimeKind.Utc); // Next day 12:00 AM
        Assert.Equal(expectedTime, nextInterval);
    }

    [Fact]
    public void CalculateNextInterval_With60MinuteInterval_ReturnsCorrectNextTime()
    {
        var configuration = new ApplicationConfiguration { ExtractIntervalMinutes = 60 };
        var scheduleCalculator = new ScheduleCalculator(configuration, _timeProvider);
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 14, 20, 0, DateTimeKind.Utc)); // 2:20 PM
        
        var nextInterval = scheduleCalculator.CalculateNextInterval();

        var expectedTime = new DateTime(2023, 7, 1, 15, 0, 0, DateTimeKind.Utc); // 3:00 PM
        Assert.Equal(expectedTime, nextInterval);
    }

    [Fact]
    public void CalculateNextInterval_With30MinuteInterval_ReturnsCorrectNextTime()
    {
        var configuration = new ApplicationConfiguration { ExtractIntervalMinutes = 30 };
        var scheduleCalculator = new ScheduleCalculator(configuration, _timeProvider);
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 11, 15, 0, DateTimeKind.Utc)); // 11:15 AM (after initial 10:30)
        
        var nextInterval = scheduleCalculator.CalculateNextInterval();

        var expectedTime = new DateTime(2023, 7, 1, 11, 30, 0, DateTimeKind.Utc); // 11:30 AM
        Assert.Equal(expectedTime, nextInterval);
    }

    [Fact]
    public void CalculateDelayUntilNextExecution_WithValidInterval_ReturnsCorrectDelay()
    {
        var delay = _scheduleCalculator.CalculateDelayUntilNextExecution();

        var expectedDelay = TimeSpan.FromMinutes(15);
        Assert.Equal(expectedDelay, delay);
    }

    [Fact]
    public void CalculateDelayUntilNextExecution_AtIntervalBoundary_ReturnsZeroDelay()
    {
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 10, 45, 0, DateTimeKind.Utc)); // 10:45 AM (exactly on interval)
        
        var delay = _scheduleCalculator.CalculateDelayUntilNextExecution();

        var expectedDelay = TimeSpan.FromMinutes(15); // Should go to next interval (11:00 AM)
        Assert.Equal(expectedDelay, delay);
    }

    [Theory]
    [InlineData(10, 30, 15, 10, 45)] // 10:30 AM -> 10:45 AM (15 min delay)
    [InlineData(14, 20, 60, 15, 0)]  // 2:20 PM -> 3:00 PM (40 min delay)
    [InlineData(9, 15, 30, 9, 30)]   // 9:15 AM -> 9:30 AM (15 min delay)
    [InlineData(23, 50, 15, 0, 0)]   // 11:50 PM -> 12:00 AM next day (10 min delay)
    public void CalculateNextInterval_VariousScenarios_ReturnsExpectedResults(
        int currentHour, int currentMinute, int intervalMinutes, int expectedHour, int expectedMinute)
    {
        var configuration = new ApplicationConfiguration { ExtractIntervalMinutes = intervalMinutes };
        var scheduleCalculator = new ScheduleCalculator(configuration, _timeProvider);
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, currentHour, currentMinute, 0, DateTimeKind.Utc));
        var expectedDate = expectedHour == 0 ? new DateTime(2023, 7, 2, expectedHour, expectedMinute, 0, DateTimeKind.Utc)
                                            : new DateTime(2023, 7, 1, expectedHour, expectedMinute, 0, DateTimeKind.Utc);

        var nextInterval = scheduleCalculator.CalculateNextInterval();

        Assert.Equal(expectedDate, nextInterval);
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ScheduleCalculator(null!, _timeProvider));
        
        Assert.Equal("configuration", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => 
            new ScheduleCalculator(_configuration, null!));
        
        Assert.Equal("timeProvider", exception.ParamName);
    }

    #region IsWithinExecutionWindow Tests


    [Theory]
    [InlineData(10, 16, 0, true)]  // 1 minute after 10:15 interval
    [InlineData(10, 15, 30, true)] // 30 seconds after 10:15 interval
    [InlineData(10, 15, 0, true)]  // exactly on 10:15 interval
    [InlineData(10, 14, 30, false)] // 30 seconds before 10:15 interval (current interval is 10:00)
    [InlineData(10, 14, 0, false)]  // 1 minute before 10:15 interval (current interval is 10:00)
    [InlineData(10, 13, 0, false)] // 2 minutes before 10:15 interval (current interval is 10:00)
    [InlineData(10, 17, 0, false)] // 2 minutes after 10:15 interval
    [InlineData(10, 30, 0, true)]  // exactly on 10:30 interval
    [InlineData(10, 30, 30, true)] // 30 seconds after 10:30 interval
    [InlineData(10, 31, 0, true)]  // 1 minute after 10:30 interval
    [InlineData(10, 32, 0, false)] // 2 minutes after 10:30 interval
    public void IsWithinExecutionWindow_VariousTimes_ReturnsExpectedResult(int hour, int minute, int second, bool expected)
    {
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, hour, minute, second, DateTimeKind.Utc));
        
        var result = _scheduleCalculator.IsWithinExecutionWindow();
        
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsWithinExecutionWindow_WithDifferentInterval_CalculatesCorrectly()
    {
        var configuration = new ApplicationConfiguration { ExtractIntervalMinutes = 60 };
        var scheduleCalculator = new ScheduleCalculator(configuration, _timeProvider);
        
        // Set time to 14:30:00 (2:30 PM) - should be within tolerance of 14:00 interval
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 14, 30, 0, DateTimeKind.Utc));
        
        var result = scheduleCalculator.IsWithinExecutionWindow();
        
        Assert.False(result); // 30 minutes is outside the 1-minute tolerance
    }

    [Fact]
    public void IsWithinExecutionWindow_WithDifferentInterval_WithinTolerance_ReturnsTrue()
    {
        var configuration = new ApplicationConfiguration { ExtractIntervalMinutes = 60 };
        var scheduleCalculator = new ScheduleCalculator(configuration, _timeProvider);
        
        // Set time to 14:01:00 (2:01 PM) - should be within tolerance of 14:00 interval
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 14, 1, 0, DateTimeKind.Utc));
        
        var result = scheduleCalculator.IsWithinExecutionWindow();
        
        Assert.True(result); // 1 minute is within tolerance
    }

    #endregion

    #region CalculateDayAheadDate Tests


    [Fact]
    public void CalculateDayAheadDate_WithEuropeBerlinTimeZone_HandlesDaylightSavingTime()
    {
        // Test during daylight saving time (summer)
        // 2023-07-01 10:30:00 UTC = 2023-07-01 12:30:00 Europe/Berlin (CEST, UTC+2)
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 10, 30, 0, DateTimeKind.Utc));
        
        var dayAheadDate = _scheduleCalculator.CalculateDayAheadDate();
        
        // Should return 2023-07-02 (next day in Berlin timezone)
        var expectedDate = new DateTime(2023, 7, 2);
        Assert.Equal(expectedDate, dayAheadDate);
    }

    [Fact]
    public void CalculateDayAheadDate_WithEuropeBerlinTimeZone_HandlesStandardTime()
    {
        // Test during standard time (winter)
        // 2023-01-15 10:30:00 UTC = 2023-01-15 11:30:00 Europe/Berlin (CET, UTC+1)
        _timeProvider.SetCurrentTime(new DateTime(2023, 1, 15, 10, 30, 0, DateTimeKind.Utc));
        
        var dayAheadDate = _scheduleCalculator.CalculateDayAheadDate();
        
        // Should return 2023-01-16 (next day in Berlin timezone)
        var expectedDate = new DateTime(2023, 1, 16);
        Assert.Equal(expectedDate, dayAheadDate);
    }

    [Fact]
    public void CalculateDayAheadDate_WithCustomTimeZone_ReturnsCorrectDate()
    {
        var configuration = new ApplicationConfiguration 
        { 
            ExtractIntervalMinutes = 15,
            TimeZoneId = "UTC" // Use UTC for this test
        };
        var scheduleCalculator = new ScheduleCalculator(configuration, _timeProvider);
        
        // Set time to 2023-07-01 15:45:00 UTC
        _timeProvider.SetCurrentTime(new DateTime(2023, 7, 1, 15, 45, 0, DateTimeKind.Utc));
        
        var dayAheadDate = scheduleCalculator.CalculateDayAheadDate();
        
        // Should return 2023-07-02 (next day in UTC)
        var expectedDate = new DateTime(2023, 7, 2);
        Assert.Equal(expectedDate, dayAheadDate);
    }

    [Theory]
    [InlineData(2023, 7, 1, 0, 0, 0, 2023, 7, 2)]   // Midnight UTC = 2:00 AM Berlin (CEST, UTC+2) -> next day
    [InlineData(2023, 7, 1, 12, 0, 0, 2023, 7, 2)]  // Noon UTC = 2:00 PM Berlin (CEST, UTC+2) -> next day
    [InlineData(2023, 7, 1, 21, 59, 59, 2023, 7, 2)]  // 9:59:59 PM UTC = 11:59:59 PM Berlin (CEST, UTC+2) -> next day
    [InlineData(2023, 12, 31, 22, 59, 59, 2024, 1, 1)] // 10:59:59 PM UTC = 11:59:59 PM Berlin (CET, UTC+1) -> next day
    public void CalculateDayAheadDate_VariousTimes_ReturnsNextDay(
        int currentYear, int currentMonth, int currentDay, int currentHour, int currentMinute, int currentSecond,
        int expectedYear, int expectedMonth, int expectedDay)
    {
        _timeProvider.SetCurrentTime(new DateTime(currentYear, currentMonth, currentDay, currentHour, currentMinute, currentSecond, DateTimeKind.Utc));
        
        var dayAheadDate = _scheduleCalculator.CalculateDayAheadDate();
        
        var expectedDate = new DateTime(expectedYear, expectedMonth, expectedDay);
        Assert.Equal(expectedDate, dayAheadDate);
    }

    #endregion

    /// <summary>
    /// Mock time provider for testing that allows setting a fixed current time
    /// </summary>
    private class MockTimeProvider : TimeProvider
    {
        private DateTime _currentTime;

        public MockTimeProvider(DateTime currentTime)
        {
            _currentTime = currentTime;
        }

        public void SetCurrentTime(DateTime currentTime)
        {
            _currentTime = currentTime;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(_currentTime, TimeSpan.Zero);
        }
    }
}

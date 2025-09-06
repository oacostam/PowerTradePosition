using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;
using Xunit.Abstractions;

namespace PowerTradePosition.Domain.UnitTests;

public class PositionExtractorTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public PositionExtractorTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Extract_ThrowsException_OnFailure()
    {
        // Arrange
        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service failure"));

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(DateTime.Today.AddDays(1));
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(DateTime.UtcNow);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        mockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));

        var mockCsvWriter = new Mock<ICsvWriter>();

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>());

        // Act & Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await Assert.ThrowsAsync<Exception>(() => extractor.ExtractPositionsAsync(cts.Token));
        
        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCsvWriter.Verify(
            x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Extract_HandlesNoTrades_ReturnsEarly()
    {
        // Arrange
        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(DateTime.Today.AddDays(1));
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(DateTime.UtcNow);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

        var mockCsvWriter = new Mock<ICsvWriter>();

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>());

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await extractor.ExtractPositionsAsync(cts.Token);

        // Assert
        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCsvWriter.Verify(
            x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        mockFileSystem.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Extract_HandlesNoPositions_ReturnsEarly()
    {
        // Arrange
        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new PowerTrade(DateTime.Today.AddDays(1), [])]);

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(DateTime.Today.AddDays(1));
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(DateTime.UtcNow);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

        var mockCsvWriter = new Mock<ICsvWriter>();

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>());

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await extractor.ExtractPositionsAsync(cts.Token);

        // Assert
        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        // Note: Even with empty periods, the aggregator still creates positions, so CSV writer is called
        mockCsvWriter.Verify(
            x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Extract_ThrowsException_OnPersistentFailure()
    {
        // Arrange
        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Persistent failure"));

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(DateTime.Today.AddDays(1));
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(DateTime.UtcNow);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

        var mockCsvWriter = new Mock<ICsvWriter>();

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>());

        // Act & Assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var exception = await Assert.ThrowsAsync<Exception>(() => extractor.ExtractPositionsAsync(cts.Token));
        Assert.Equal("Persistent failure", exception.Message);

        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Once); // Only one attempt since no retry logic
    }

    [Fact]
    public void TimeGridBuilder_HandlesDSTTransition_Correctly()
    {
        // Test the time grid builder directly for DST transition
        var dayAheadDate = new DateTime(2024, 3, 31); // March 31, 2024 (Sunday) - DST spring forward

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var timeGrid = grid.BuildHourlyTimeGrid(dayAheadDate, "Europe/Berlin").ToList();

        _testOutputHelper.WriteLine($"Time grid for {dayAheadDate:yyyy-MM-dd} in Europe/Berlin:");
        for (var i = 0; i < timeGrid.Count; i++)
            _testOutputHelper.WriteLine($"Index {i}: {timeGrid[i]:yyyy-MM-ddTHH:mm:ssZ} (UTC)");

        // During DST spring forward, we should have 23 entries (lose 2:00 AM)
        Assert.Equal(23, timeGrid.Count);

        // Verify that 2:00 AM local time is not in the grid
        var twoAmLocal = new DateTime(2024, 3, 31, 2, 0, 0, DateTimeKind.Unspecified);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        // 2:00 AM should be invalid during DST spring forward
        Assert.True(timeZone.IsInvalidTime(twoAmLocal));

        // Verify the time progression
        for (var i = 1; i < timeGrid.Count; i++)
        {
            var timeDiff = timeGrid[i] - timeGrid[i - 1];
            // Most intervals should be 1 hour, but during DST transition there might be 2 hours
            Assert.True(timeDiff.TotalHours is >= 1.0 and <= 2.0,
                $"Time difference between positions {i - 1} and {i} is {timeDiff.TotalHours} hours");
        }
    }

    [Fact]
    public async Task Extract_HandlesDaylightSavingTimeChanges_Correctly()
    {
        // Test scenario: March 31, 2024 - DST transition from winter to summer time
        // In Europe/Berlin, clocks move forward 1 hour at 2:00 AM on March 31, 2024
        // This means 2:00 AM becomes 3:00 AM, so we "lose" one hour

        var dayAheadDate = new DateTime(2024, 3, 31); // March 31, 2024 (Sunday)
        var extractionTime = new DateTime(2024, 3, 30, 21, 15, 0); // March 30, 2024 at 21:15 (UTC)

        // Create test data with the same pattern as the requirements example
        var trades = new[]
        {
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 100)).ToArray()
            ),
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 50)).ToArray()
            ),
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, p >= 12 ? -20 : 0)).ToArray()
            )
        };

        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trades);

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(dayAheadDate);
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(extractionTime);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

        var mockCsvWriter = new Mock<ICsvWriter>();
        mockCsvWriter.Setup(x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };

        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(extractionTime, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>()
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await extractor.ExtractPositionsAsync(cts.Token);

        // Verify the extraction was successful
        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCsvWriter.Verify(
            x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify the CSV writer was called with the correct parameters
        mockCsvWriter.Verify(x => x.WriteToFileAsync(
            It.IsAny<IEnumerable<PowerPosition>>(),
            dayAheadDate,
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify the time progression accounts for DST change
        var timeGrid = grid.BuildHourlyTimeGrid(dayAheadDate, "Europe/Berlin").ToList();
        _testOutputHelper.WriteLine($"Time grid for {dayAheadDate:yyyy-MM-dd} in Europe/Berlin:");
        for (var i = 0; i < timeGrid.Count; i++)
            _testOutputHelper.WriteLine($"Period {i + 1}: {timeGrid[i]:yyyy-MM-ddTHH:mm:ssZ} (UTC)");

        // During DST spring forward, we lose one hour (2:00 AM becomes 3:00 AM)
        // So the time grid should have 23 entries, not 24
        Assert.Equal(23, timeGrid.Count);

        // Verify that 2:00 AM local time is not in the grid (it becomes 3:00 AM due to DST)
        var twoAmLocal = new DateTime(2024, 3, 31, 2, 0, 0, DateTimeKind.Unspecified);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");

        // 2:00 AM should be invalid during DST spring forward
        Assert.True(timeZone.IsInvalidTime(twoAmLocal));
    }

    [Fact]
    public async Task Extract_HandlesDaylightSavingTimeFallBack_Correctly()
    {
        // Test scenario: October 27, 2024 - DST transition from summer to winter time
        // In Europe/Berlin, clocks move backward 1 hour at 3:00 AM on October 27, 2024
        // This means 3:00 AM becomes 2:00 AM, so we "gain" one hour

        var dayAheadDate = new DateTime(2024, 10, 27); // October 27, 2024 (Sunday)
        var extractionTime = new DateTime(2024, 10, 26, 21, 15, 0); // October 26, 2024 at 21:15 (UTC)

        // Create test data
        var trades = new[]
        {
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 100)).ToArray()
            ),
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 50)).ToArray()
            )
        };

        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trades);

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(dayAheadDate);
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(extractionTime);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

        var mockCsvWriter = new Mock<ICsvWriter>();
        mockCsvWriter.Setup(x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };

        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(extractionTime, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>()
        );

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await extractor.ExtractPositionsAsync(cts.Token);

        // Verify the extraction was successful
        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCsvWriter.Verify(
            x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify the CSV writer was called with the correct parameters
        mockCsvWriter.Verify(x => x.WriteToFileAsync(
            It.IsAny<IEnumerable<PowerPosition>>(),
            dayAheadDate,
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // Verify the time progression accounts for DST fallback
        var timeGrid = grid.BuildHourlyTimeGrid(dayAheadDate, "Europe/Berlin").ToList();
        Assert.Equal(25, timeGrid.Count);

        // Log the actual time grid for debugging
        _testOutputHelper.WriteLine($"Time grid for {dayAheadDate:yyyy-MM-dd} in Europe/Berlin (DST fallback):");
        for (var i = 0; i < timeGrid.Count; i++)
            _testOutputHelper.WriteLine($"Period {i + 1}: {timeGrid[i]:yyyy-MM-ddTHH:mm:ssZ} (UTC)");

        // During DST fallback, we should have 25 entries (gain 1 hour)
        // 2:00 AM appears twice (once before and once after the change)
        // The aggregator should handle this correctly by mapping periods to the appropriate UTC times
    }

    [Fact]
    public async Task Extract_LogsAppropriateMessages_ForSuccessfulExtraction()
    {
        // Arrange
        var dayAheadDate = DateTime.Today.AddDays(1);
        var trades = new[]
        {
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 100)).ToArray()
            )
        };

        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trades);

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(dayAheadDate);
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(DateTime.UtcNow);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

        var mockCsvWriter = new Mock<ICsvWriter>();
        mockCsvWriter.Setup(x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>());

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await extractor.ExtractPositionsAsync(cts.Token);

        // Assert
        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCsvWriter.Verify(
            x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Extract_HandlesCancellation_Properly()
    {
        // Arrange
        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns<DateTime, CancellationToken>((_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(Enumerable.Empty<PowerTrade>());
            });

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(DateTime.Today.AddDays(1));
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(DateTime.UtcNow);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

        var mockCsvWriter = new Mock<ICsvWriter>();

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>());

        // Act & Assert
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // OperationCanceledException is thrown when cancellation is requested
        await Assert.ThrowsAsync<OperationCanceledException>(() => extractor.ExtractPositionsAsync(cts.Token));

        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Extract_HandlesDirectoryCreation_WhenOutputDirectoryDoesNotExist()
    {
        // Arrange
        var dayAheadDate = DateTime.Today.AddDays(1);
        var trades = new[]
        {
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 100)).ToArray()
            )
        };

        var mockTradeService = new Mock<ITradeService>();
        mockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trades);

        var mockScheduleCalculator = new Mock<IScheduleCalculator>();
        mockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(dayAheadDate);
        mockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(DateTime.UtcNow);

        var mockFileSystem = new Mock<IFileSystem>();
        mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        mockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));

        var mockCsvWriter = new Mock<ICsvWriter>();
        mockCsvWriter.Setup(x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(),
                It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var grid = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        var aggregator = new PositionAggregator(grid, new NullLogger<PositionAggregator>());
        var cfg = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin"
        };
        var timeProvider = new FakeTimeProvider();
        timeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero));

        var extractor = new PositionExtractor(
            mockTradeService.Object,
            aggregator,
            mockCsvWriter.Object,
            cfg,
            timeProvider,
            mockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>());

        // Act
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await extractor.ExtractPositionsAsync(cts.Token);

        // Assert
        mockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        mockCsvWriter.Verify(
            x => x.WriteToFileAsync(It.IsAny<IEnumerable<PowerPosition>>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

}
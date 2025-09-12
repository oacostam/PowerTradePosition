using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.UnitTests.Fixtures;

/// <summary>
/// Specialized fixture for PositionExtractor tests that extends the base fixture
/// with PositionExtractor-specific mocks and setup methods.
/// </summary>
public class PositionExtractorFixture : BaseTestFixture
{
    public Mock<IScheduleCalculator> MockScheduleCalculator { get; }
    public Mock<ICsvWriter> MockCsvWriter { get; }
    public PositionExtractor PositionExtractor { get; }

    public PositionExtractorFixture()
    {
        // Initialize PositionExtractor-specific mocks
        MockScheduleCalculator = new Mock<IScheduleCalculator>();
        MockCsvWriter = new Mock<ICsvWriter>();

        // Setup PositionExtractor-specific default behaviors
        SetupPositionExtractorDefaultBehaviors();

        // Create the PositionExtractor with all dependencies
        PositionExtractor = new PositionExtractor(
            MockTradeService.Object,
            PositionAggregator,
            MockCsvWriter.Object,
            Configuration,
            TimeProvider,
            MockScheduleCalculator.Object,
            new NullLogger<PositionExtractor>());
    }

    private void SetupPositionExtractorDefaultBehaviors()
    {
        // Default schedule calculator behavior
        MockScheduleCalculator.Setup(x => x.CalculateDayAheadDate())
            .Returns(DateTime.Today.AddDays(1));
        MockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone())
            .Returns(DateTime.UtcNow);

        // Default CSV writer behavior - successful write
        MockCsvWriter.Setup(x => x.WriteToFileAsync(
                It.IsAny<IEnumerable<PowerPosition>>(),
                It.IsAny<DateTime>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Sets up the fixture for a successful extraction scenario
    /// </summary>
    public void SetupSuccessfulExtraction(DateTime dayAheadDate, PowerTrade[]? trades = null)
    {
        trades ??= CreateExampleTrades(dayAheadDate);
        
        MockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trades);
        
        MockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(dayAheadDate);
        MockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(DateTime.UtcNow);
    }

    /// <summary>
    /// Sets up the fixture for a cancellation scenario
    /// </summary>
    public void SetupCancellation()
    {
        MockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns<DateTime, CancellationToken>((_, ct) =>
            {
                ct.ThrowIfCancellationRequested();
                return Task.FromResult(Enumerable.Empty<PowerTrade>());
            });
    }

    /// <summary>
    /// Sets up the fixture for DST testing scenarios
    /// </summary>
    public void SetupDstTest(DateTime dayAheadDate, DateTime extractionTime)
    {
        var trades = CreateDstTestTrades(dayAheadDate);
        
        MockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(trades);
        
        MockScheduleCalculator.Setup(x => x.CalculateDayAheadDate()).Returns(dayAheadDate);
        MockScheduleCalculator.Setup(x => x.GetCurrentTimeInConfiguredTimeZone()).Returns(extractionTime);
        
        // Only set time if it's not going backwards (FakeTimeProvider doesn't allow this)
        var currentTime = TimeProvider.GetUtcNow();
        if (extractionTime > currentTime.DateTime)
        {
            TimeProvider.SetUtcNow(new DateTimeOffset(extractionTime, TimeSpan.Zero));
        }
    }

    /// <summary>
    /// Verifies that the CSV writer was called exactly once
    /// </summary>
    public void VerifyCsvWriterCalledOnce()
    {
        MockCsvWriter.Verify(x => x.WriteToFileAsync(
            It.IsAny<IEnumerable<PowerPosition>>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that the CSV writer was never called
    /// </summary>
    public void VerifyCsvWriterNeverCalled()
    {
        MockCsvWriter.Verify(x => x.WriteToFileAsync(
            It.IsAny<IEnumerable<PowerPosition>>(),
            It.IsAny<DateTime>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Resets all mocks including PositionExtractor-specific ones
    /// </summary>
    public override void ResetMocks()
    {
        base.ResetMocks();
        MockScheduleCalculator.Reset();
        MockCsvWriter.Reset();
        
        // Re-setup PositionExtractor-specific behaviors
        SetupPositionExtractorDefaultBehaviors();
    }
}

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;
using PowerTradePosition.Console;
using Moq;

namespace PowerTradePosition.Domain.UnitTests;

public class ScheduledExtractorTests
{
    [Fact]
    public async Task RunsInitialExtraction_OnStart()
    {
        // Arrange
        var mockExtractor = new Mock<IPositionExtractor>();
        var callCount = 0;
        mockExtractor.Setup(x => x.ExtractPositionsAsync(It.IsAny<CancellationToken>()))
            .Callback<CancellationToken>(_ => callCount++)
            .Returns(Task.CompletedTask);

        var cfg = new ApplicationConfiguration { ExtractIntervalMinutes = 60 };
        var timeProvider = new FakeTimeProvider();
        var scheduleCalculator = new ScheduleCalculator(cfg, timeProvider);
        var svc = new ScheduledExtractor(mockExtractor.Object, scheduleCalculator, new NullLogger<ScheduledExtractor>());

        using var cts = new CancellationTokenSource();

        var runTask = svc.StartAsync(cts.Token);

        await Task.Delay(100, cts.Token); // allow initial run
        await svc.StopAsync(cts.Token);
        await runTask;

        Assert.True(callCount >= 1);
        mockExtractor.Verify(x => x.ExtractPositionsAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }

    [Fact]
    public async Task HandlesInitialExtractionFailure_Gracefully()
    {
        // Arrange
        var mockExtractor = new Mock<IPositionExtractor>();
        mockExtractor.Setup(x => x.ExtractPositionsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated failure"));

        var cfg = new ApplicationConfiguration { ExtractIntervalMinutes = 60 };
        var timeProvider = new FakeTimeProvider();
        var scheduleCalculator = new ScheduleCalculator(cfg, timeProvider);
        var svc = new ScheduledExtractor(mockExtractor.Object, scheduleCalculator, new NullLogger<ScheduledExtractor>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)); // Short timeout for test

        var runTask = svc.StartAsync(cts.Token);

        try
        {
            await runTask;
        }
        catch (OperationCanceledException)
        {
            // Expected due to timeout
        }

        // Verify that the initial extraction was attempted
        mockExtractor.Verify(x => x.ExtractPositionsAsync(It.IsAny<CancellationToken>()), Times.AtLeast(1));
    }
}
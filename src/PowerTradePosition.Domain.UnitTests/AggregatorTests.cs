using PowerTradePosition.Domain.UnitTests.Fixtures;

namespace PowerTradePosition.Domain.UnitTests;

public class AggregatorTests : IClassFixture<AggregatorFixture>
{
    private readonly AggregatorFixture _fixture;

    public AggregatorTests(AggregatorFixture fixture)
    {
        _fixture = fixture;
    }
    [Fact]
    public void AggregatePositions_SumsByHour_AndConvertsToUtc_Berlin()
    {
        // Arrange
        _fixture.ResetMocks();
        var dayAheadDate = new DateTime(2023, 7, 2);
        var trades = _fixture.CreateVariableVolumeTrades(dayAheadDate);

        // Act
        var positions = _fixture.PositionAggregator.AggregatePositionsByHour(trades, "Europe/Berlin").ToList();

        // Assert
        var (isValid, errorMessage) = _fixture.CheckPositionAggregation(positions, 24);
        Assert.True(isValid, errorMessage);
        
        // Spot check first hours per example (Berlin is UTC+2 in July)
        Assert.Equal(new DateTime(2023, 7, 1, 22, 0, 0, DateTimeKind.Utc), positions[0].DateTime);
        Assert.Equal(150, positions[0].Volume);
        Assert.Equal(new DateTime(2023, 7, 1, 23, 0, 0, DateTimeKind.Utc), positions[1].DateTime);
        Assert.Equal(150, positions[1].Volume);
    }

    [Fact]
    public void AggregatePositions_HandlesMultipleTrades_SumsVolumes()
    {
        // Arrange
        _fixture.ResetMocks();
        var dayAheadDate = new DateTime(2023, 7, 2);
        var trades = _fixture.CreateMultipleTradesForSummation(dayAheadDate);

        // Act
        var positions = _fixture.PositionAggregator.AggregatePositionsByHour(trades, "Europe/Berlin").ToList();

        // Assert
        var (isValid, errorMessage) = _fixture.CheckPositionAggregation(positions, 24);
        Assert.True(isValid, errorMessage);
        
        var (isVolumeValid, volumeErrorMessage) = _fixture.CheckSummationVolumes(positions, 80);
        Assert.True(isVolumeValid, volumeErrorMessage);
    }
}
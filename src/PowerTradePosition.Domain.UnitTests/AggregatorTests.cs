using Microsoft.Extensions.Logging.Abstractions;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.UnitTests;

public class AggregatorTests
{
    [Fact]
    public void AggregatePositions_SumsByHour_AndConvertsToUtc_Berlin()
    {
        var logger = new NullLogger<TimeGridBuilder>();
        ITimeGridBuilder grid = new TimeGridBuilder(logger);
        var aggLogger = new NullLogger<PositionAggregator>();
        var aggregator = new PositionAggregator(grid, aggLogger);

        var trades = new List<PowerTrade>
        {
            new(
                new DateTime(2023, 7, 2),
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, p <= 11 ? 150 : 80)).ToArray()
            )
        };

        var positions = aggregator.AggregatePositionsByHour(trades, "Europe/Berlin").ToList();

        Assert.Equal(24, positions.Count);
        // Spot check first hours per example (Berlin is UTC+2 in July)
        Assert.Equal(new DateTime(2023, 7, 1, 22, 0, 0, DateTimeKind.Utc), positions[0].DateTime);
        Assert.Equal(150, positions[0].Volume);
        Assert.Equal(new DateTime(2023, 7, 1, 23, 0, 0, DateTimeKind.Utc), positions[1].DateTime);
        Assert.Equal(150, positions[1].Volume);
    }

    [Fact]
    public void AggregatePositions_HandlesMultipleTrades_SumsVolumes()
    {
        var logger = new NullLogger<TimeGridBuilder>();
        ITimeGridBuilder grid = new TimeGridBuilder(logger);
        var aggLogger = new NullLogger<PositionAggregator>();
        var aggregator = new PositionAggregator(grid, aggLogger);

        var trade1 = new PowerTrade(
            new DateTime(2023, 7, 2),
            Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 100)).ToArray()
        );
        var trade2 = new PowerTrade(
            new DateTime(2023, 7, 2),
            Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, -20)).ToArray()
        );

        var positions = aggregator.AggregatePositionsByHour([trade1, trade2], "Europe/Berlin").ToList();

        Assert.All(positions, p => Assert.Equal(80, p.Volume));
    }
}
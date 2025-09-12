namespace PowerTradePosition.Domain.UnitTests.Fixtures;

/// <summary>
/// Specialized fixture for Aggregator tests that extends the base fixture
/// with Aggregator-specific setup methods.
/// </summary>
public class AggregatorFixture : BaseTestFixture
{
    /// <summary>
    /// Resets all mocks including Aggregator-specific ones
    /// </summary>
    public override void ResetMocks()
    {
        base.ResetMocks();
        // Aggregator doesn't have additional mocks, just uses base mocks
    }
}


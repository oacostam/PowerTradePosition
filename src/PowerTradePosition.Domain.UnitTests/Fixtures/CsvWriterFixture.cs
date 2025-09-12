using Microsoft.Extensions.Logging.Abstractions;
using PowerTradePosition.Domain.Domain;

namespace PowerTradePosition.Domain.UnitTests.Fixtures;

/// <summary>
/// Specialized fixture for CsvWriter tests that extends the base fixture
/// with CsvWriter-specific setup methods.
/// </summary>
public class CsvWriterFixture : BaseTestFixture
{
    public CsvWriter CsvWriter { get; }

    public CsvWriterFixture()
    {
        // Create the CsvWriter with mocked dependencies
        CsvWriter = new CsvWriter(
            MockFileSystem.Object,
            new NullLogger<CsvWriter>());
    }

    /// <summary>
    /// Resets all mocks including CsvWriter-specific ones
    /// </summary>
    public override void ResetMocks()
    {
        base.ResetMocks();
        // CsvWriter doesn't have additional mocks, just uses base mocks
    }
}


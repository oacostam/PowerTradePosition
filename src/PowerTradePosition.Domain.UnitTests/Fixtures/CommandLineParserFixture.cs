using Microsoft.Extensions.Logging.Abstractions;
using PowerTradePosition.Console;

namespace PowerTradePosition.Domain.UnitTests.Fixtures;

/// <summary>
/// Specialized fixture for CommandLineParser tests that extends the base fixture
/// with CommandLineParser-specific setup methods.
/// </summary>
public class CommandLineParserFixture : BaseTestFixture
{
    public CommandLineParser CommandLineParser { get; }

    public CommandLineParserFixture()
    {
        // Create the CommandLineParser with mocked dependencies
        CommandLineParser = new CommandLineParser(
            MockConfiguration.Object,
            new NullLogger<CommandLineParser>());
    }

    /// <summary>
    /// Resets all mocks including CommandLineParser-specific ones
    /// </summary>
    public override void ResetMocks()
    {
        base.ResetMocks();
        // CommandLineParser doesn't have additional mocks, just uses base mocks
    }
}


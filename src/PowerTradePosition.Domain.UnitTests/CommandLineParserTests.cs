using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PowerTradePosition.Console;

namespace PowerTradePosition.Domain.UnitTests;

public class CommandLineParserTests
{
    [Fact]
    public void CommandLine_TakesPrecedence_OverConfigFile()
    {
        // Arrange
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["OutputFolderPath"]).Returns("C:/fromconfig");
        mockConfig.Setup(x => x["ExtractIntervalMinutes"]).Returns("60");
        mockConfig.Setup(x => x["TimeZoneId"]).Returns("Europe/London");

        var logger = new NullLogger<CommandLineParser>();
        var parser = new CommandLineParser(mockConfig.Object, logger);

        var result = parser.ParseConfiguration([
            "--output-folder", "C:/fromargs", "--interval", "30", "--timezone", "Europe/Berlin"
        ]);

        Assert.Equal("C:/fromargs", result.OutputFolderPath);
        Assert.Equal(30, result.ExtractIntervalMinutes);
        Assert.Equal("Europe/Berlin", result.TimeZoneId);
    }

    [Fact]
    public void Defaults_AreApplied_WhenNotProvided()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(x => x["OutputFolderPath"]).Returns((string?)null);
        mockConfig.Setup(x => x["ExtractIntervalMinutes"]).Returns((string?)null);
        mockConfig.Setup(x => x["TimeZoneId"]).Returns((string?)null);

        var logger = new NullLogger<CommandLineParser>();
        var parser = new CommandLineParser(mockConfig.Object, logger);

        var result = parser.ParseConfiguration(Array.Empty<string>());

        Assert.NotNull(result.OutputFolderPath);
        Assert.True(result.ExtractIntervalMinutes > 0);
        Assert.Equal("Europe/Berlin", result.TimeZoneId);
    }
}
using PowerTradePosition.Domain.UnitTests.Fixtures;

namespace PowerTradePosition.Domain.UnitTests;

public class CommandLineParserTests : IClassFixture<CommandLineParserFixture>
{
    private readonly CommandLineParserFixture _fixture;

    public CommandLineParserTests(CommandLineParserFixture fixture)
    {
        _fixture = fixture;
    }
    [Fact]
    public void CommandLine_TakesPrecedence_OverConfigFile()
    {
        // Arrange
        _fixture.ResetMocks();
        _fixture.SetupConfigFileValues();
        var args = _fixture.CreateOverrideArgs();

        // Act
        var result = _fixture.CommandLineParser.ParseConfiguration(args);

        // Assert
        var (isValid, errorMessage) = _fixture.CheckCommandLinePrecedence(result);
        Assert.True(isValid, errorMessage);
    }

    [Fact]
    public void Defaults_AreApplied_WhenNotProvided()
    {
        // Arrange
        _fixture.ResetMocks();
        _fixture.SetupNullConfiguration();
        var args = _fixture.CreateEmptyArgs();

        // Act
        var result = _fixture.CommandLineParser.ParseConfiguration(args);

        // Assert
        var (isValid, errorMessage) = _fixture.CheckDefaultConfiguration(result);
        Assert.True(isValid, errorMessage);
    }
}
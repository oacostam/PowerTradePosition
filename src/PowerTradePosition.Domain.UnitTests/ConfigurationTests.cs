using PowerTradePosition.Domain.Domain;

namespace PowerTradePosition.Domain.UnitTests;

public class ConfigurationTests
{
    [Fact]
    public void Constructor_WithValidValues_SetsPropertiesCorrectly()
    {
        var config = new ApplicationConfiguration(
            outputFolderPath: "/test/path",
            extractIntervalMinutes: 30,
            timeZoneId: "UTC"
        );
        Assert.Equal("/test/path", config.OutputFolderPath);
        Assert.Equal(30, config.ExtractIntervalMinutes);
        Assert.Equal("UTC", config.TimeZoneId);
    }

    [Fact]
    public void Constructor_WithDefaultValues_UsesDefaultValues()
    {
        var config = new ApplicationConfiguration();

        Assert.Equal("Output", config.OutputFolderPath);
        Assert.Equal(15, config.ExtractIntervalMinutes);
        Assert.Equal("Europe/Berlin", config.TimeZoneId);
    }

    [Fact]
    public void ExtractIntervalMinutes_WithValidValue_SetsProperty()
    {
        var config = new ApplicationConfiguration
        {
            ExtractIntervalMinutes = 45
        };

        Assert.Equal(45, config.ExtractIntervalMinutes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-15)]
    public void ExtractIntervalMinutes_WithInvalidValue_ThrowsArgumentException(int invalidValue)
    {
        var config = new ApplicationConfiguration();

        var exception = Assert.Throws<ArgumentException>(() => 
            config.ExtractIntervalMinutes = invalidValue);
        
        Assert.Contains("must be greater than 0", exception.Message);
        Assert.Equal("value", exception.ParamName);
    }


    [Fact]
    public void OutputFolderPath_WithValidValue_SetsProperty()
    {
        var config = new ApplicationConfiguration
        {
            OutputFolderPath = "/new/path"
        };

        Assert.Equal("/new/path", config.OutputFolderPath);
    }

    [Fact]
    public void TimeZoneId_WithValidValue_SetsProperty()
    {
        var config = new ApplicationConfiguration
        {
            TimeZoneId = "America/New_York"
        };
        Assert.Equal("America/New_York", config.TimeZoneId);
    }
}

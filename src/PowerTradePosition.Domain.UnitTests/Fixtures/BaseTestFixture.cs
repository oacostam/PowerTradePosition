using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Moq;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.UnitTests.Fixtures;

/// <summary>
/// Base fixture that provides common test data, mocks, and real objects
/// shared across all test classes. Follows the principle of eliminating
/// code duplication while maintaining clear separation of concerns.
/// </summary>
public abstract class BaseTestFixture : IDisposable
{
    // Common Mocks - used by most test classes
    public Mock<ITradeService> MockTradeService { get; }
    public Mock<IFileSystem> MockFileSystem { get; }
    public Mock<IConfiguration> MockConfiguration { get; }

    // Common Real Objects - used by most test classes
    public ApplicationConfiguration Configuration { get; }
    public FakeTimeProvider TimeProvider { get; }
    public TimeGridBuilder TimeGridBuilder { get; }
    public PositionAggregator PositionAggregator { get; }

    // Common Test Data
    public string TempDirectory { get; }

    protected BaseTestFixture()
    {
        // Initialize common mocks
        MockTradeService = new Mock<ITradeService>();
        MockFileSystem = new Mock<IFileSystem>();
        MockConfiguration = new Mock<IConfiguration>();

        // Initialize common real objects with proper logging
        TimeGridBuilder = new TimeGridBuilder(new NullLogger<TimeGridBuilder>());
        PositionAggregator = new PositionAggregator(TimeGridBuilder, new NullLogger<PositionAggregator>());
        
        // Default configuration matching application requirements
        Configuration = new ApplicationConfiguration
        {
            OutputFolderPath = Path.GetTempPath(),
            TimeZoneId = "Europe/Berlin", // As specified in requirements
            ExtractIntervalMinutes = 15
        };

        // Initialize time provider with a fixed time for consistent testing
        TimeProvider = new FakeTimeProvider();
        TimeProvider.SetUtcNow(new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero));

        // Get temp directory for file operations
        TempDirectory = Path.GetTempPath();

        // Setup default mock behaviors
        SetupDefaultMockBehaviors();
    }

    protected void SetupDefaultMockBehaviors()
    {
        // Default trade service behavior - returns empty trades
        MockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Default file system behavior - directory exists
        MockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);

        // Default configuration values matching application requirements
        MockConfiguration.Setup(x => x["OutputFolderPath"]).Returns("Output");
        MockConfiguration.Setup(x => x["ExtractIntervalMinutes"]).Returns("15");
        MockConfiguration.Setup(x => x["TimeZoneId"]).Returns("Europe/Berlin");
    }

    #region Common PowerTrade Creation Methods

    /// <summary>
    /// Creates test trades matching the application requirements example
    /// </summary>
    public PowerTrade[] CreateExampleTrades(DateTime dayAheadDate)
    {
        return
        [
            // First trade: 100 volume for all periods
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 100)).ToArray()
            ),
            // Second trade: 50 volume for all periods
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 50)).ToArray()
            ),
            // Third trade: -20 volume for periods 12-24, 0 for periods 1-11
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, p >= 12 ? -20 : 0)).ToArray()
            )
        ];
    }

    /// <summary>
    /// Creates trades with different volume patterns for testing aggregation
    /// </summary>
    public PowerTrade[] CreateVariableVolumeTrades(DateTime dayAheadDate)
    {
        return
        [
            // Trade with higher volume in first half of day
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, p <= 11 ? 150 : 80)).ToArray()
            )
        ];
    }

    /// <summary>
    /// Creates multiple trades for testing volume summation
    /// </summary>
    public PowerTrade[] CreateMultipleTradesForSummation(DateTime dayAheadDate)
    {
        return
        [
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 100)).ToArray()
            ),
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, -20)).ToArray()
            )
        ];
    }

    /// <summary>
    /// Creates trades for DST testing scenarios
    /// </summary>
    public PowerTrade[] CreateDstTestTrades(DateTime dayAheadDate)
    {
        return
        [
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 100)).ToArray()
            ),
            new PowerTrade(
                dayAheadDate,
                Enumerable.Range(1, 24).Select(p => new PowerPeriod(p, 50)).ToArray()
            )
        ];
    }

    #endregion

    #region Common PowerPosition Creation Methods

    /// <summary>
    /// Creates positions with decimal values to test formatting
    /// </summary>
    public List<PowerPosition> CreateDecimalPositions()
    {
        return new List<PowerPosition>
        {
            new(new DateTime(2023, 7, 1, 22, 0, 0, DateTimeKind.Utc), 150.0),
            new(new DateTime(2023, 7, 1, 23, 0, 0, DateTimeKind.Utc), 80.5)
        };
    }

    /// <summary>
    /// Creates empty positions list for testing header-only scenarios
    /// </summary>
    public List<PowerPosition> CreateEmptyPositions()
    {
        return new List<PowerPosition>();
    }

    #endregion

    #region Common Setup Methods

    /// <summary>
    /// Sets up the fixture for a service failure scenario
    /// </summary>
    public void SetupServiceFailure(string errorMessage = "Service failure")
    {
        MockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception(errorMessage));
    }

    /// <summary>
    /// Sets up the fixture for a no trades scenario
    /// </summary>
    public void SetupNoTrades()
    {
        MockTradeService.Setup(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    /// <summary>
    /// Sets up the fixture for a directory creation scenario
    /// </summary>
    public void SetupDirectoryCreation()
    {
        MockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(false);
        MockFileSystem.Setup(x => x.CreateDirectory(It.IsAny<string>()));
    }

    /// <summary>
    /// Sets up the fixture for a directory exists scenario
    /// </summary>
    public void SetupDirectoryExists()
    {
        MockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
    }

    /// <summary>
    /// Sets up configuration with valid values from config file
    /// </summary>
    public void SetupConfigFileValues()
    {
        MockConfiguration.Setup(x => x["OutputFolderPath"]).Returns("C:/fromconfig");
        MockConfiguration.Setup(x => x["ExtractIntervalMinutes"]).Returns("60");
        MockConfiguration.Setup(x => x["TimeZoneId"]).Returns("Europe/London");
    }

    /// <summary>
    /// Sets up configuration with null values to test default behavior
    /// </summary>
    public void SetupNullConfiguration()
    {
        MockConfiguration.Setup(x => x["OutputFolderPath"]).Returns((string?)null);
        MockConfiguration.Setup(x => x["ExtractIntervalMinutes"]).Returns((string?)null);
        MockConfiguration.Setup(x => x["TimeZoneId"]).Returns((string?)null);
    }

    #endregion

    #region Common Command Line Argument Creation Methods

    /// <summary>
    /// Creates command line arguments for testing
    /// </summary>
    public string[] CreateCommandLineArgs(string? outputFolder = null, string? interval = null, string? timezone = null)
    {
        var args = new List<string>();
        
        if (!string.IsNullOrEmpty(outputFolder))
        {
            args.Add("--output-folder");
            args.Add(outputFolder);
        }
        
        if (!string.IsNullOrEmpty(interval))
        {
            args.Add("--interval");
            args.Add(interval);
        }
        
        if (!string.IsNullOrEmpty(timezone))
        {
            args.Add("--timezone");
            args.Add(timezone);
        }
        
        return args.ToArray();
    }

    /// <summary>
    /// Creates command line arguments that override config file values
    /// </summary>
    public string[] CreateOverrideArgs()
    {
        return CreateCommandLineArgs("C:/fromargs", "30", "Europe/Berlin");
    }

    /// <summary>
    /// Creates empty command line arguments
    /// </summary>
    public string[] CreateEmptyArgs()
    {
        return Array.Empty<string>();
    }

    #endregion

    #region Common Utility Methods

    /// <summary>
    /// Creates a cancellation token source with a reasonable timeout for tests
    /// </summary>
    public CancellationTokenSource CreateCancellationTokenSource(TimeSpan? timeout = null)
    {
        return new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Creates a cancellation token source that is already cancelled
    /// </summary>
    public CancellationTokenSource CreateCancelledTokenSource()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts;
    }

    /// <summary>
    /// Generates expected filename based on application requirements convention
    /// </summary>
    public string GenerateExpectedFilename(DateTime dayAheadDate, DateTime extractionTime)
    {
        return $"PowerPosition_{dayAheadDate:yyyyMMdd}_{extractionTime:yyyyMMddHHmm}.csv";
    }

    /// <summary>
    /// Gets the expected file path for a given filename
    /// </summary>
    public string GetExpectedFilePath(string filename)
    {
        return Path.Combine(TempDirectory, filename);
    }

    #endregion

    #region Common Validation Helper Methods

    /// <summary>
    /// Helper method to check if positions are properly aggregated by hour and converted to UTC
    /// Returns validation results instead of asserting
    /// </summary>
    public (bool isValid, string errorMessage) CheckPositionAggregation(List<PowerPosition> positions, int expectedCount)
    {
        if (positions.Count != expectedCount)
            return (false, $"Expected {expectedCount} positions, but got {positions.Count}");
        
        // Check that all positions are in UTC
        var nonUtcPositions = positions.Where(p => p.DateTime.Kind != DateTimeKind.Utc).ToList();
        if (nonUtcPositions.Any())
            return (false, $"Found {nonUtcPositions.Count} positions that are not in UTC");
        
        // Check that positions are properly ordered by time
        for (var i = 1; i < positions.Count; i++)
        {
            if (positions[i].DateTime <= positions[i - 1].DateTime)
                return (false, $"Position {i} datetime {positions[i].DateTime} should be after position {i-1} datetime {positions[i-1].DateTime}");
        }
        
        return (true, string.Empty);
    }

    /// <summary>
    /// Helper method to check if positions match expected volumes for summation scenario
    /// Returns validation results instead of asserting
    /// </summary>
    public (bool isValid, string errorMessage) CheckSummationVolumes(List<PowerPosition> positions, double expectedVolume)
    {
        var incorrectVolumes = positions.Where(p => Math.Abs(p.Volume - expectedVolume) > 0.001).ToList();
        if (!incorrectVolumes.Any()) return (true, string.Empty);
        var firstIncorrect = incorrectVolumes.First();
        return (false, $"Expected all positions to have volume {expectedVolume}, but found {firstIncorrect.Volume} at {firstIncorrect.DateTime}");
    }

    /// <summary>
    /// Helper method to check if a file exists and contains the expected content
    /// Returns validation results instead of asserting
    /// </summary>
    public async Task<(bool isValid, string errorMessage)> CheckFileContentAsync(string filePath, List<string> expectedLines, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            return (false, $"Expected file {filePath} to exist");

        var lines = await File.ReadAllLinesAsync(filePath, cancellationToken);
        
        if (lines.Length != expectedLines.Count)
            return (false, $"Expected {expectedLines.Count} lines, but file has {lines.Length} lines");
        
        for (var i = 0; i < expectedLines.Count; i++)
        {
            if (lines[i] != expectedLines[i])
                return (false, $"Line {i + 1} mismatch. Expected: '{expectedLines[i]}', Actual: '{lines[i]}'");
        }
        
        return (true, string.Empty);
    }

    /// <summary>
    /// Helper method to check CSV format requirements (semicolon separator, decimal point, ISO 8601 datetime)
    /// Returns validation results instead of asserting
    /// </summary>
    public (bool isValid, string errorMessage) CheckCsvFormat(string line)
    {
        // Should not contain commas (semicolon separator requirement)
        if (line.Contains(','))
            return (false, "CSV line should not contain commas (should use semicolon separator)");
        
        // Should contain semicolon separator
        if (!line.Contains(';'))
            return (false, "CSV line should contain semicolon separator");
        
        // If it's a data line (not header), validate datetime format
        if (!line.StartsWith("Datetime"))
        {
            var parts = line.Split(';');
            if (parts.Length != 2)
                return (false, $"CSV data line should have exactly 2 parts separated by semicolon, but found {parts.Length}");
            
            // Validate ISO 8601 datetime format - should end with Z for UTC
            if (!DateTime.TryParse(parts[0], out _))
                return (false, $"Invalid datetime format: {parts[0]}");
            
            if (!parts[0].EndsWith("Z", StringComparison.OrdinalIgnoreCase))
                return (false, $"Datetime should end with 'Z' for UTC: {parts[0]}");
            
            // Validate decimal format
            if (!double.TryParse(parts[1], out _))
                return (false, $"Invalid decimal format: {parts[1]}");
        }
        
        return (true, string.Empty);
    }

    /// <summary>
    /// Helper method to check filename format according to application requirements
    /// Returns validation results instead of asserting
    /// </summary>
    public (bool isValid, string errorMessage) CheckFilenameFormat(string filename)
    {
        // Format: PowerPosition_YYYYMMDD_YYYYMMDDHHMM.csv
        if (!filename.StartsWith("PowerPosition_"))
            return (false, "Filename should start with 'PowerPosition_'");
        
        if (!filename.EndsWith(".csv"))
            return (false, "Filename should end with '.csv'");
        
        var parts = filename.Replace("PowerPosition_", "").Replace(".csv", "").Split('_');
        if (parts.Length != 2)
            return (false, $"Filename should have format PowerPosition_YYYYMMDD_YYYYMMDDHHMM.csv, but found {parts.Length} parts");
        
        // First part: YYYYMMDD (day-ahead date)
        if (parts[0].Length != 8)
            return (false, $"First date part should be 8 characters (YYYYMMDD), but found {parts[0].Length}");
        
        if (!int.TryParse(parts[0], out _))
            return (false, $"First date part should be numeric (YYYYMMDD): {parts[0]}");
        
        // Second part: YYYYMMDDHHMM (extraction timestamp)
        if (parts[1].Length != 12)
            return (false, $"Second timestamp part should be 12 characters (YYYYMMDDHHMM), but found {parts[1].Length}");
        
        if (!long.TryParse(parts[1], out _))
            return (false, $"Second timestamp part should be numeric (YYYYMMDDHHMM): {parts[1]}");
        
        return (true, string.Empty);
    }

    /// <summary>
    /// Helper method to check if the parsed configuration uses default values
    /// Returns validation results instead of asserting
    /// </summary>
    public (bool isValid, string errorMessage) CheckDefaultConfiguration(ApplicationConfiguration config)
    {
        if (string.IsNullOrEmpty(config.OutputFolderPath))
            return (false, "OutputFolderPath should not be null or empty");
        
        if (config.ExtractIntervalMinutes <= 0)
            return (false, $"ExtractIntervalMinutes should be greater than 0, but found {config.ExtractIntervalMinutes}");
        
        if (config.TimeZoneId != "Europe/Berlin")
            return (false, $"TimeZoneId should be 'Europe/Berlin', but found '{config.TimeZoneId}'");
        
        return (true, string.Empty);
    }

    /// <summary>
    /// Helper method to check if command line arguments take precedence over config file values
    /// Returns validation results instead of asserting
    /// </summary>
    public (bool isValid, string errorMessage) CheckCommandLinePrecedence(ApplicationConfiguration config)
    {
        if (config.OutputFolderPath != "C:/fromargs")
            return (false, $"Expected OutputFolderPath to be 'C:/fromargs', but found '{config.OutputFolderPath}'");
        
        if (config.ExtractIntervalMinutes != 30)
            return (false, $"Expected ExtractIntervalMinutes to be 30, but found {config.ExtractIntervalMinutes}");
        
        if (config.TimeZoneId != "Europe/Berlin")
            return (false, $"Expected TimeZoneId to be 'Europe/Berlin', but found '{config.TimeZoneId}'");
        
        return (true, string.Empty);
    }

    #endregion

    #region Common Verification Methods

    /// <summary>
    /// Verifies that the trade service was called exactly once
    /// </summary>
    public void VerifyTradeServiceCalledOnce()
    {
        MockTradeService.Verify(x => x.GetTradesAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that directory creation was never called
    /// </summary>
    public void VerifyDirectoryCreationNeverCalled()
    {
        MockFileSystem.Verify(x => x.CreateDirectory(It.IsAny<string>()), Times.Never);
    }

    #endregion

    #region Common Reset and Cleanup Methods

    /// <summary>
    /// Resets all mocks to their default state - call this at the beginning of each test
    /// </summary>
    public virtual void ResetMocks()
    {
        MockTradeService.Reset();
        MockFileSystem.Reset();
        MockConfiguration.Reset();
        
        // Re-setup default behaviors
        SetupDefaultMockBehaviors();
    }

    /// <summary>
    /// Cleans up test files that might have been created
    /// </summary>
    public void CleanupTestFiles()
    {
        try
        {
            var testFiles = Directory.GetFiles(TempDirectory, "PowerPosition_*.csv");
            foreach (var file in testFiles)
            {
                try 
                { 
                    File.Delete(file); 
                } 
                catch 
                { 
                    // Ignore cleanup errors
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    public virtual void Dispose()
    {
        CleanupTestFiles();
    }

    #endregion
}


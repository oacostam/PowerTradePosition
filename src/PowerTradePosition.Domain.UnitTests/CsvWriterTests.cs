using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.UnitTests.Fixtures;

namespace PowerTradePosition.Domain.UnitTests;

public class CsvWriterTests : IClassFixture<CsvWriterFixture>
{
    private readonly CsvWriterFixture _fixture;

    public CsvWriterTests(CsvWriterFixture fixture)
    {
        _fixture = fixture;
    }
    [Fact]
    public async Task WriteToFile_WritesHeaderAndFormattedRows()
    {
        // Arrange
        _fixture.ResetMocks();
        _fixture.SetupDirectoryExists();
        var positions = _fixture.CreateDecimalPositions();
        var dayAheadDate = new DateTime(2023, 7, 2);
        var extractionTime = new DateTime(2023, 7, 1, 19, 15, 0);
        var expectedFileName = _fixture.GenerateExpectedFilename(dayAheadDate, extractionTime);
        var expectedFilePath = _fixture.GetExpectedFilePath(expectedFileName);

        try
        {
            // Act
            using var cts = _fixture.CreateCancellationTokenSource();
            await _fixture.CsvWriter.WriteToFileAsync(positions, dayAheadDate, extractionTime, _fixture.TempDirectory, cts.Token);

            // Assert
            Assert.True(File.Exists(expectedFilePath), $"Expected file {expectedFilePath} to exist");

            var expectedLines = new List<string>
            {
                "Datetime;Volume",
                "2023-07-01T22:00:00Z;150.00",
                "2023-07-01T23:00:00Z;80.50"
            };

            var (isValid, errorMessage) = await _fixture.CheckFileContentAsync(expectedFilePath, expectedLines, cts.Token);
            Assert.True(isValid, errorMessage);

            // Validate CSV format requirements
            var (isFormatValid1, formatError1) = _fixture.CheckCsvFormat(expectedLines[1]);
            Assert.True(isFormatValid1, formatError1);
            
            var (isFormatValid2, formatError2) = _fixture.CheckCsvFormat(expectedLines[2]);
            Assert.True(isFormatValid2, formatError2);
        }
        finally
        {
            // Clean up the test file
            try
            {
                if (File.Exists(expectedFilePath)) File.Delete(expectedFilePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Theory]
    [InlineData(2023, 7, 2, 2023, 7, 1, 19, 15, "PowerPosition_20230702_202307011915.csv")]
    [InlineData(2014, 12, 20, 2014, 12, 20, 18, 37, "PowerPosition_20141220_201412201837.csv")]
    public void GenerateFileName_MatchesConvention(int y1, int m1, int d1, int y2, int m2, int d2, int h2, int min2,
        string expected)
    {
        // Arrange
        _fixture.ResetMocks();
        var dayAhead = new DateTime(y1, m1, d1);
        var extractionUtc = new DateTime(y2, m2, d2, h2, min2, 0, DateTimeKind.Utc);

        // Act
        var name = _fixture.CsvWriter.GenerateFileName(dayAhead, extractionUtc);

        // Assert
        Assert.Equal(expected, name);
        var (isValid, errorMessage) = _fixture.CheckFilenameFormat(name);
        Assert.True(isValid, errorMessage);
    }

    [Fact]
    public void GenerateFileName_WithUserScenario_ProducesCorrectFilename()
    {
        // Arrange - Based on the user's actual output scenario
        // Execution time: 2025-09-06 11:17:43.821 (appears to be local time)
        // Day-ahead date: 2025-09-07
        // Expected filename: PowerPosition_20250907_202509060917.csv (if 09:17 was local time)
        // But if 09:17 was UTC, then the filename should be: PowerPosition_20250907_202509060917.csv
        
        _fixture.ResetMocks();
        var dayAheadDate = new DateTime(2025, 9, 7); // 2025-09-07
        var extractionTimeUtc = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Utc); // 09:17:43 UTC

        // Act
        var actualFilename = _fixture.CsvWriter.GenerateFileName(dayAheadDate, extractionTimeUtc);

        // Assert
        var expectedFilename = "PowerPosition_20250907_202509060917.csv";
        Assert.Equal(expectedFilename, actualFilename);
        var (isValid, errorMessage) = _fixture.CheckFilenameFormat(actualFilename);
        Assert.True(isValid, errorMessage);
    }

    [Theory]
    [InlineData(2025, 9, 6, 0, 0, 0, "PowerPosition_20250907_202509060000.csv")] // Midnight UTC
    [InlineData(2025, 9, 6, 23, 59, 59, "PowerPosition_20250907_202509062359.csv")] // End of day UTC
    [InlineData(2025, 12, 31, 23, 59, 59, "PowerPosition_20260101_202512312359.csv")] // Year boundary
    [InlineData(2025, 1, 1, 0, 0, 0, "PowerPosition_20250102_202501010000.csv")] // New year
    public void GenerateFileName_WithEdgeCases_ProducesCorrectFilename(int year, int month, int day, int hour, int minute, int second, string expected)
    {
        // Arrange
        _fixture.ResetMocks();
        // Calculate the correct day-ahead date based on the extraction time
        var extractionTimeUtc = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        var dayAheadDate = extractionTimeUtc.Date.AddDays(1);

        // Act
        var actualFilename = _fixture.CsvWriter.GenerateFileName(dayAheadDate, extractionTimeUtc);

        // Assert
        Assert.Equal(expected, actualFilename);
        var (isValid, errorMessage) = _fixture.CheckFilenameFormat(actualFilename);
        Assert.True(isValid, errorMessage);
    }

    [Fact]
    public void GenerateFileName_WithDifferentDateTimeKinds_HandlesCorrectly()
    {
        // Arrange
        _fixture.ResetMocks();
        var dayAheadDate = new DateTime(2025, 9, 7);
        
        // Test with different DateTimeKind values
        var extractionTimeUtc = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Utc);
        var extractionTimeUnspecified = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Unspecified);
        var extractionTimeLocal = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Local);

        // Act
        var filenameUtc = _fixture.CsvWriter.GenerateFileName(dayAheadDate, extractionTimeUtc);
        var filenameUnspecified = _fixture.CsvWriter.GenerateFileName(dayAheadDate, extractionTimeUnspecified);
        var filenameLocal = _fixture.CsvWriter.GenerateFileName(dayAheadDate, extractionTimeLocal);

        // Assert - All should produce the same filename since we're only using the date/time components
        var expectedFilename = "PowerPosition_20250907_202509060917.csv";
        Assert.Equal(expectedFilename, filenameUtc);
        Assert.Equal(expectedFilename, filenameUnspecified);
        Assert.Equal(expectedFilename, filenameLocal);
        
        // Validate format for all
        var (isValidUtc, errorUtc) = _fixture.CheckFilenameFormat(filenameUtc);
        Assert.True(isValidUtc, errorUtc);
        
        var (isValidUnspecified, errorUnspecified) = _fixture.CheckFilenameFormat(filenameUnspecified);
        Assert.True(isValidUnspecified, errorUnspecified);
        
        var (isValidLocal, errorLocal) = _fixture.CheckFilenameFormat(filenameLocal);
        Assert.True(isValidLocal, errorLocal);
    }

    [Fact]
    public void GenerateFileName_FormatValidation_MatchesSpecification()
    {
        // Arrange
        _fixture.ResetMocks();
        var dayAheadDate = new DateTime(2025, 9, 7);
        var extractionTimeUtc = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Utc);

        // Act
        var filename = _fixture.CsvWriter.GenerateFileName(dayAheadDate, extractionTimeUtc);

        // Assert - Validate the filename format matches specification
        var expectedFilename = "PowerPosition_20250907_202509060917.csv";
        Assert.Equal(expectedFilename, filename);
        
        // Use fixture validation method
        var (isValid, errorMessage) = _fixture.CheckFilenameFormat(filename);
        Assert.True(isValid, errorMessage);
    }

    [Fact]
    public async Task WriteToFile_WithEmptyPositions_CreatesFileWithHeaderOnly()
    {
        // Arrange
        _fixture.ResetMocks();
        _fixture.SetupDirectoryExists();
        var positions = _fixture.CreateEmptyPositions();
        var dayAheadDate = new DateTime(2023, 7, 2);
        var extractionTime = new DateTime(2023, 7, 1, 19, 15, 0);
        var expectedFileName = _fixture.GenerateExpectedFilename(dayAheadDate, extractionTime);
        var expectedFilePath = _fixture.GetExpectedFilePath(expectedFileName);

        try
        {
            // Act
            using var cts = _fixture.CreateCancellationTokenSource();
            await _fixture.CsvWriter.WriteToFileAsync(positions, dayAheadDate, extractionTime, _fixture.TempDirectory, cts.Token);

            // Assert
            Assert.True(File.Exists(expectedFilePath), $"Expected file {expectedFilePath} to exist");

            var expectedLines = new List<string> { "Datetime;Volume" };
            var (isValid, errorMessage) = await _fixture.CheckFileContentAsync(expectedFilePath, expectedLines, cts.Token);
            Assert.True(isValid, errorMessage);
        }
        finally
        {
            // Clean up the test file
            try
            {
                if (File.Exists(expectedFilePath)) File.Delete(expectedFilePath);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task WriteToFile_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        _fixture.ResetMocks();
        var positions = new List<PowerPosition>
        {
            new(new DateTime(2023, 7, 1, 22, 0, 0, DateTimeKind.Utc), 150)
        };

        var dayAheadDate = new DateTime(2023, 7, 2);
        var extractionTime = new DateTime(2023, 7, 1, 19, 15, 0);

        using var cts = _fixture.CreateCancelledTokenSource();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => 
            _fixture.CsvWriter.WriteToFileAsync(positions, dayAheadDate, extractionTime, _fixture.TempDirectory, cts.Token));
    }
}

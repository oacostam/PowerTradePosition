using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Domain.UnitTests;

public class CsvWriterTests
{
    [Fact]
    public async Task WriteToFile_WritesHeaderAndFormattedRows()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        try
        {
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            
            var configuration = new ApplicationConfiguration
            {
                OutputFolderPath = tempDir,
                TimeZoneId = "Europe/Berlin"
            };
            
            var logger = new NullLogger<CsvWriter>();
            var writer = new CsvWriter(mockFileSystem.Object, configuration, logger);

            var positions = new List<PowerPosition>
            {
                new(new DateTime(2023, 7, 1, 22, 0, 0, DateTimeKind.Utc), 150),
                new(new DateTime(2023, 7, 1, 23, 0, 0, DateTimeKind.Utc), 80.5)
            };

            var dayAheadDate = new DateTime(2023, 7, 2);
            var extractionTime = new DateTime(2023, 7, 1, 19, 15, 0);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await writer.WriteToFileAsync(positions, dayAheadDate, extractionTime, tempDir, cts.Token);

            // The file should be created with the generated filename
            const string expectedFileName = "PowerPosition_20230702_202307011915.csv";
            var expectedFilePath = Path.Combine(tempDir, expectedFileName);
            
            Assert.True(File.Exists(expectedFilePath), $"Expected file {expectedFilePath} to exist");

            var lines = await File.ReadAllLinesAsync(expectedFilePath, cts.Token);

            Assert.Equal("Datetime;Volume", lines[0]);
            Assert.Equal("2023-07-01T22:00:00Z;150.00", lines[1]);
            Assert.Equal("2023-07-01T23:00:00Z;80.50", lines[2]);
            Assert.DoesNotContain(',', lines[1]);
            Assert.DoesNotContain(',', lines[2]);
            Assert.Contains('.', lines[1]);
            Assert.Contains('.', lines[2]);

            // Clean up the test file
            if (File.Exists(expectedFilePath)) File.Delete(expectedFilePath);
        }
        catch
        {
            // Clean up any files that might have been created
            var testFiles = Directory.GetFiles(tempDir, "PowerPosition_*.csv");
            foreach (var file in testFiles)
            {
                // ReSharper disable once EmptyGeneralCatchClause
                try { File.Delete(file); } catch { }
            }
            throw;
        }
    }

    [Theory]
    [InlineData(2023, 7, 2, 2023, 7, 1, 19, 15, "PowerPosition_20230702_202307011915.csv")]
    [InlineData(2014, 12, 20, 2014, 12, 20, 18, 37, "PowerPosition_20141220_201412201837.csv")]
    public void GenerateFileName_MatchesConvention(int y1, int m1, int d1, int y2, int m2, int d2, int h2, int min2,
        string expected)
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var configuration = new ApplicationConfiguration();
        var logger = new NullLogger<CsvWriter>();
        var writer = new CsvWriter(mockFileSystem.Object, configuration, logger);

        var dayAhead = new DateTime(y1, m1, d1);
        var extractionUtc = new DateTime(y2, m2, d2, h2, min2, 0, DateTimeKind.Utc);

        // Act
        var name = writer.GenerateFileName(dayAhead, extractionUtc);

        // Assert
        Assert.Equal(expected, name);
    }

    [Fact]
    public void GenerateFileName_WithUserScenario_ProducesCorrectFilename()
    {
        // Arrange - Based on the user's actual output scenario
        // Execution time: 2025-09-06 11:17:43.821 (appears to be local time)
        // Day-ahead date: 2025-09-07
        // Expected filename: PowerPosition_20250907_202509060917.csv (if 09:17 was local time)
        // But if 09:17 was UTC, then the filename should be: PowerPosition_20250907_202509060917.csv
        
        var mockFileSystem = new Mock<IFileSystem>();
        var configuration = new ApplicationConfiguration();
        var logger = new NullLogger<CsvWriter>();
        var writer = new CsvWriter(mockFileSystem.Object, configuration, logger);

        var dayAheadDate = new DateTime(2025, 9, 7); // 2025-09-07
        var extractionTimeUtc = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Utc); // 09:17:43 UTC

        // Act
        var actualFilename = writer.GenerateFileName(dayAheadDate, extractionTimeUtc);

        // Assert
        var expectedFilename = "PowerPosition_20250907_202509060917.csv";
        Assert.Equal(expectedFilename, actualFilename);
    }

    [Theory]
    [InlineData(2025, 9, 6, 0, 0, 0, "PowerPosition_20250907_202509060000.csv")] // Midnight UTC
    [InlineData(2025, 9, 6, 23, 59, 59, "PowerPosition_20250907_202509062359.csv")] // End of day UTC
    [InlineData(2025, 12, 31, 23, 59, 59, "PowerPosition_20260101_202512312359.csv")] // Year boundary
    [InlineData(2025, 1, 1, 0, 0, 0, "PowerPosition_20250102_202501010000.csv")] // New year
    public void GenerateFileName_WithEdgeCases_ProducesCorrectFilename(int year, int month, int day, int hour, int minute, int second, string expected)
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var configuration = new ApplicationConfiguration();
        var logger = new NullLogger<CsvWriter>();
        var writer = new CsvWriter(mockFileSystem.Object, configuration, logger);

        // Calculate the correct day-ahead date based on the extraction time
        var extractionTimeUtc = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        var dayAheadDate = extractionTimeUtc.Date.AddDays(1);

        // Act
        var actualFilename = writer.GenerateFileName(dayAheadDate, extractionTimeUtc);

        // Assert
        Assert.Equal(expected, actualFilename);
    }

    [Fact]
    public void GenerateFileName_WithDifferentDateTimeKinds_HandlesCorrectly()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var configuration = new ApplicationConfiguration();
        var logger = new NullLogger<CsvWriter>();
        var writer = new CsvWriter(mockFileSystem.Object, configuration, logger);

        var dayAheadDate = new DateTime(2025, 9, 7);
        
        // Test with different DateTimeKind values
        var extractionTimeUtc = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Utc);
        var extractionTimeUnspecified = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Unspecified);
        var extractionTimeLocal = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Local);

        // Act
        var filenameUtc = writer.GenerateFileName(dayAheadDate, extractionTimeUtc);
        var filenameUnspecified = writer.GenerateFileName(dayAheadDate, extractionTimeUnspecified);
        var filenameLocal = writer.GenerateFileName(dayAheadDate, extractionTimeLocal);

        // Assert - All should produce the same filename since we're only using the date/time components
        var expectedFilename = "PowerPosition_20250907_202509060917.csv";
        Assert.Equal(expectedFilename, filenameUtc);
        Assert.Equal(expectedFilename, filenameUnspecified);
        Assert.Equal(expectedFilename, filenameLocal);
    }

    [Fact]
    public void GenerateFileName_FormatValidation_MatchesSpecification()
    {
        // Arrange
        var mockFileSystem = new Mock<IFileSystem>();
        var configuration = new ApplicationConfiguration();
        var logger = new NullLogger<CsvWriter>();
        var writer = new CsvWriter(mockFileSystem.Object, configuration, logger);

        var dayAheadDate = new DateTime(2025, 9, 7);
        var extractionTimeUtc = new DateTime(2025, 9, 6, 9, 17, 43, DateTimeKind.Utc);

        // Act
        var filename = writer.GenerateFileName(dayAheadDate, extractionTimeUtc);

        // Assert - Validate the filename format matches specification
        // Format: PowerPosition_YYYYMMDD_YYYYMMDDHHMM.csv
        Assert.StartsWith("PowerPosition_", filename);
        Assert.EndsWith(".csv", filename);
        
        var parts = filename.Replace("PowerPosition_", "").Replace(".csv", "").Split('_');
        Assert.Equal(2, parts.Length);
        
        // First part: YYYYMMDD (day-ahead date)
        Assert.Equal("20250907", parts[0]);
        Assert.Equal(8, parts[0].Length);
        Assert.True(int.TryParse(parts[0], out _));
        
        // Second part: YYYYMMDDHHMM (extraction timestamp)
        Assert.Equal("202509060917", parts[1]);
        Assert.Equal(12, parts[1].Length);
        // Note: The timestamp part is too large for int32, so we'll use long instead
        Assert.True(long.TryParse(parts[1], out _), $"Failed to parse '{parts[1]}' as long integer");
        
        // Additional validation: ensure the filename matches expected format
        var expectedFilename = "PowerPosition_20250907_202509060917.csv";
        Assert.Equal(expectedFilename, filename);
    }

    [Fact]
    public async Task WriteToFile_WithEmptyPositions_CreatesFileWithHeaderOnly()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        try
        {
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(x => x.DirectoryExists(It.IsAny<string>())).Returns(true);
            
            var configuration = new ApplicationConfiguration
            {
                OutputFolderPath = tempDir,
                TimeZoneId = "Europe/Berlin"
            };
            
            var logger = new NullLogger<CsvWriter>();
            var writer = new CsvWriter(mockFileSystem.Object, configuration, logger);

            var dayAheadDate = new DateTime(2023, 7, 2);
            var extractionTime = new DateTime(2023, 7, 1, 19, 15, 0);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await writer.WriteToFileAsync(new List<PowerPosition>(0), dayAheadDate, extractionTime, tempDir, cts.Token);

            const string expectedFileName = "PowerPosition_20230702_202307011915.csv";
            var expectedFilePath = Path.Combine(tempDir, expectedFileName);
            
            Assert.True(File.Exists(expectedFilePath), $"Expected file {expectedFilePath} to exist");

            var lines = await File.ReadAllLinesAsync(expectedFilePath, cts.Token);

            Assert.Single(lines);
            Assert.Equal("Datetime;Volume", lines[0]);

            // Clean up the test file
            if (File.Exists(expectedFilePath)) File.Delete(expectedFilePath);
        }
        catch
        {
            // Clean up any files that might have been created
            var testFiles = Directory.GetFiles(tempDir, "PowerPosition_*.csv");
            foreach (var file in testFiles)
            {
                // ReSharper disable once EmptyGeneralCatchClause
                try { File.Delete(file); } catch { }
            }
            throw;
        }
    }

    [Fact]
    public async Task WriteToFile_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var tempDir = Path.GetTempPath();
        var mockFileSystem = new Mock<IFileSystem>();
        var configuration = new ApplicationConfiguration
        {
            OutputFolderPath = tempDir,
            TimeZoneId = "Europe/Berlin"
        };
        var logger = new NullLogger<CsvWriter>();
        var writer = new CsvWriter(mockFileSystem.Object, configuration, logger);

        var positions = new List<PowerPosition>
        {
            new(new DateTime(2023, 7, 1, 22, 0, 0, DateTimeKind.Utc), 150)
        };

        var dayAheadDate = new DateTime(2023, 7, 2);
        var extractionTime = new DateTime(2023, 7, 1, 19, 15, 0);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => 
            writer.WriteToFileAsync(positions, dayAheadDate, extractionTime, tempDir, cts.Token));
    }
}

using Microsoft.Extensions.Logging;
using PowerTradePosition.Domain.Interfaces;
using System.Globalization;

namespace PowerTradePosition.Domain.Domain;

public class CsvWriter(IFileSystem fileSystem, ILogger<CsvWriter> logger) : ICsvWriter
{

    public async Task WriteToFileAsync(IEnumerable<PowerPosition> positions, DateTime dayAheadDate, DateTime extractionTime, string outputFolderPath, CancellationToken ct)
    {
        try
        {
            var powerPositions = positions as PowerPosition[] ?? positions.ToArray();
            
            // Generate filename internally
            var fileName = GenerateFileName(dayAheadDate, extractionTime);
            var filePath = Path.Combine(outputFolderPath, fileName);
            
            logger.LogInformation("Writing {Count} positions to CSV file: {FilePath}", powerPositions.Length,
                filePath);

            var lines = new List<string>
            {
                "Datetime;Volume" // Header with semicolon separator
            };

            // For a fix amount of data it works, for bigger files a streaming approach would be better
            lines.AddRange(from position in powerPositions.OrderBy(p => p.DateTime)
                let datetimeStr = position.DateTime.ToString("yyyy-MM-ddTHH:mm:ssZ")
                let volumeStr = position.Volume.ToString("F2", CultureInfo.InvariantCulture)
                select $"{datetimeStr};{volumeStr}");

            // Ensure output directory exists
            if (!fileSystem.DirectoryExists(outputFolderPath))
                fileSystem.CreateDirectory(outputFolderPath);

            await File.WriteAllLinesAsync(filePath, lines, ct);

            logger.LogInformation("Successfully wrote CSV file: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error writing CSV file for day-ahead date {DayAheadDate}", dayAheadDate.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    public string GenerateFileName(DateTime dayAheadDate, DateTime extractionTime)
    {
        // Format: PowerPosition_YYYYMMDD_YYYYMMDDHHMM.csv
        var dayAheadStr = dayAheadDate.ToString("yyyyMMdd");
        var extractionStr = extractionTime.ToString("yyyyMMddHHmm");

        return $"PowerPosition_{dayAheadStr}_{extractionStr}.csv";
    }
}
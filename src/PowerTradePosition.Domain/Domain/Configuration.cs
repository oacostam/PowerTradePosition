namespace PowerTradePosition.Domain.Domain;

public class ApplicationConfiguration
{
    private int _extractIntervalMinutes = 15;

    public string OutputFolderPath { get; set; } = "Output";
    
    public int ExtractIntervalMinutes
    {
        get => _extractIntervalMinutes;
        set
        {
            if (value <= 0)
                throw new ArgumentException("Extract interval must be greater than 0", nameof(value));
            _extractIntervalMinutes = value;
        }
    }
    
    public string TimeZoneId { get; set; } = "Europe/Berlin";

    public ApplicationConfiguration()
    {
    }

    public ApplicationConfiguration(
        string outputFolderPath = "Output",
        int extractIntervalMinutes = 15,
        string timeZoneId = "Europe/Berlin")
    {
        OutputFolderPath = outputFolderPath;
        ExtractIntervalMinutes = extractIntervalMinutes;
        TimeZoneId = timeZoneId;
    }
}

public record CommandLineOptions(
    string? OutputFolderPath = null,
    int? ExtractIntervalMinutes = null,
    string? TimeZoneId = null);
using System.Diagnostics.CodeAnalysis;
using CommandLine;

namespace PowerTradePosition.Console;

[ExcludeFromCodeCoverage]
public class CommandLineOptions
{
    [Option('o', "output-folder", Required = false, HelpText = "Output folder for CSV files")]
    public string? OutputFolderPath { get; set; }

    [Option('i', "interval", Required = false, HelpText = "Extract interval in minutes (default: 15)")]
    public int? ExtractIntervalMinutes { get; set; }

    [Option('t', "timezone", Required = false, HelpText = "Time zone ID (default: Europe/Berlin)")]
    public string? TimeZoneId { get; set; }

    [Option('h', "help", Required = false, HelpText = "Show this help message")]
    public bool Help { get; set; }
}

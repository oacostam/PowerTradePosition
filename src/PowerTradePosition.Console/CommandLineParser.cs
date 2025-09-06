using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.Console;

public class CommandLineParser : ICommandLineParser
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CommandLineParser> _logger;

    public CommandLineParser(IConfiguration configuration, ILogger<CommandLineParser> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public ApplicationConfiguration ParseConfiguration(string[] args)
    {
        try
        {
            // Parse command line arguments
            var commandLineOptions = ParseCommandLineArgs(args);

            // Load from configuration file
            var fileConfig = LoadFromConfigurationFile();

            // Merge configurations (command line takes precedence)
            var config = new ApplicationConfiguration
            {
                OutputFolderPath = commandLineOptions.OutputFolderPath ?? fileConfig.OutputFolderPath,
                ExtractIntervalMinutes = commandLineOptions.ExtractIntervalMinutes ?? fileConfig.ExtractIntervalMinutes,
                TimeZoneId = commandLineOptions.TimeZoneId ?? fileConfig.TimeZoneId
            };

            // Validate configuration
            ValidateConfiguration(config);

            _logger.LogInformation(
                "Configuration loaded - OutputFolder: {OutputFolder}, Interval: {Interval} minutes, TimeZone: {TimeZone}",
                config.OutputFolderPath, config.ExtractIntervalMinutes, config.TimeZoneId);

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            throw;
        }
    }

    private static CommandLineOptions ParseCommandLineArgs(string[] args)
    {
        // Check for help flag first
        if (args.Contains("--help") || args.Contains("-h"))
        {
            ShowHelp();
            Environment.Exit(0);
        }

        var result = Parser.Default.ParseArguments<CommandLineOptions>(args);
        
        if (result.Errors.Any())
        {
            // Show help and exit if there are parsing errors
            System.Console.WriteLine("Invalid command line arguments. Use --help for usage information.");
            Environment.Exit(1);
        }

        return result.Value ?? new CommandLineOptions();
    }

    private ApplicationConfiguration LoadFromConfigurationFile()
    {
        var config = new ApplicationConfiguration();

        // Try to load from appsettings.json
        var outputFolder = _configuration["OutputFolderPath"];
        if (!string.IsNullOrEmpty(outputFolder)) 
            config.OutputFolderPath = outputFolder;
        else
            config.OutputFolderPath = "Output"; // Default fallback

        if (int.TryParse(_configuration["ExtractIntervalMinutes"], out var interval))
            config.ExtractIntervalMinutes = interval;

        var timeZone = _configuration["TimeZoneId"];
        if (!string.IsNullOrEmpty(timeZone)) 
            config.TimeZoneId = timeZone;

        return config;
    }

    private static void ValidateConfiguration(ApplicationConfiguration config)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(config.OutputFolderPath))
            errors.Add("Output folder path is required");

        if (string.IsNullOrWhiteSpace(config.TimeZoneId))
            errors.Add("Time zone ID is required");

        // Validate that the timezone exists on the system
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(config.TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            errors.Add($"Time zone '{config.TimeZoneId}' not found on the system");
        }

        if (errors.Any())
        {
            System.Console.WriteLine("Configuration validation failed:");
            foreach (var error in errors)
            {
                System.Console.WriteLine($"  - {error}");
            }
            Environment.Exit(1);
        }
    }

    private static void ShowHelp()
    {
        System.Console.WriteLine("Power Trade Position Extractor");
        System.Console.WriteLine("Usage: PowerTradePosition.Console [options]");
        System.Console.WriteLine();
        System.Console.WriteLine("Options:");
        System.Console.WriteLine("  -o, --output-folder <path>    Output folder for CSV files (default: Output)");
        System.Console.WriteLine("  -i, --interval <minutes>      Extract interval in minutes (default: 15)");
        System.Console.WriteLine("  -t, --timezone <id>           Time zone ID (default: Europe/Berlin)");
        System.Console.WriteLine("  -h, --help                    Show this help message");
        System.Console.WriteLine();
        System.Console.WriteLine("Examples:");
        System.Console.WriteLine("  PowerTradePosition.Console --output-folder C:\\Output --interval 30");
        System.Console.WriteLine("  PowerTradePosition.Console -o /var/output -i 60 -t Europe/London");
        System.Console.WriteLine();
        System.Console.WriteLine("Configuration can also be provided via appsettings.json file.");
        System.Console.WriteLine("Command line arguments take precedence over configuration file values.");
        System.Console.WriteLine("If no output folder is specified, the default 'Output' folder will be used.");
    }
}

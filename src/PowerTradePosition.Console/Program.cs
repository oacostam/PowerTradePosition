using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using PowerTradePosition.DataAccess;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace PowerTradePosition.Console;



[ExcludeFromCodeCoverage]
public class Program
{
    private static class ServiceMode
    {
        public const string Normal = "Normal";
        public const string Test = "Test";
        public const string Error = "Error";
    }
    public static async Task Main(string[] args)
    {
        Environment.SetEnvironmentVariable("SERVICE_MODE", ServiceMode.Normal);

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((_, services) =>
            {
                // Domain Services
                services.AddScoped<ITimeGridBuilder, TimeGridBuilder>();
                services.AddScoped<IPositionAggregator, PositionAggregator>();
                services.AddScoped<ICsvWriter, CsvWriter>();
                services.AddScoped<IFileSystem, FileSystem>();
                services.AddSingleton(TimeProvider.System);
                services.AddScoped<IScheduleCalculator, ScheduleCalculator>();
                services.AddScoped<IPositionExtractor, PositionExtractor>();
                services.AddPowerTradeServices();
                services.AddHostedService<ScheduledExtractor>();
                services.AddSingleton<ICommandLineParser, CommandLineParser>();
                services.AddSingleton(serviceProvider =>
                {
                    var commandLineParser = serviceProvider.GetRequiredService<ICommandLineParser>();
                    return commandLineParser.ParseConfiguration(args);
                });
                services.Configure<SimpleConsoleFormatterOptions>(options =>
                {
                    options.TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff ";
                    options.UseUtcTimestamp = false;
                    options.IncludeScopes = true;
                });
            })
            .ConfigureLogging((context, logging) =>
            {
                // Clear all default providers and add only console logging
                logging.ClearProviders();
                logging.AddConsole(options =>
                {
                    options.FormatterName = ConsoleFormatterNames.Simple;
                });
                logging.SetMinimumLevel(context.HostingEnvironment.IsDevelopment()
                    ? LogLevel.Debug
                    : LogLevel.Information);
            })
            .UseConsoleLifetime()
            .Build();

        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        try
        {
            logger.LogInformation("Power Trade Position Extractor starting...");
            logger.LogInformation("Press Ctrl+C to stop the application");
            logger.LogInformation("Running with SERVICE_MODE: {SERVICE_MODE}", Environment.GetEnvironmentVariable("SERVICE_MODE"));
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            // Log the error before disposing the app
            logger.LogError(ex, "An error occurred while running the application");
            Environment.Exit(1);
        }
        finally
        {
            await host.StopAsync();
        }
    }
}
# Power Trade Position Extractor

A .NET 9 console application that extracts and aggregates power trade positions, generating hourly consolidated reports in CSV format.

## Overview

Power traders need an intra-day report that provides their consolidated day-ahead power position. This application generates an hourly aggregated volume report and saves it to a CSV file, following a configurable schedule.

## Features

- **Scheduled Extraction**: Runs at configurable intervals (default: every 15 minutes)
- **Retry Mechanism**: Built-in retry logic to ensure no scheduled extracts are missed
- **Time Zone Support**: Handles Europe/Berlin timezone with proper UTC conversion
- **CSV Output**: Generates CSV files with ISO 8601 datetime format and semicolon separators
- **Flexible Configuration**: Supports command-line arguments and configuration files
- **Production Logging**: Comprehensive logging for production support and debugging
- **Centralized Scheduling**: Dedicated `ScheduleCalculator` class
- **Configuration Validation**: Property-level validation with CommandLineParser package

## Requirements

- .NET 9.0 or higher
- Windows (due to PowerService.dll dependency)
- PowerService.dll assembly (provided in lib folder)

## Project Structure

```
PowerTradePosition/
├── src/
│   ├── PowerTradePosition.Console/          # Console application entry point
│   ├── PowerTradePosition.Domain/           # Domain models and business logic
│   │   ├── Domain/                         # Domain entities and services
│   │   └── Interfaces/                     # Domain interfaces
│   ├── PowerTradePosition.DataAccess/       # Data access layer
│   ├── PowerTradePosition.Domain.UnitTests/ # Unit tests
│   └── PowerTradePosition.sln              # Solution file
├── lib/                                    # External dependencies (PowerService.dll)
└── doc/                                    # Documentation
```

## Architecture

The solution follows SOLID principles and implements several design patterns:

- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection
- **Repository Pattern**: ITradeService interface for data access
- **Strategy Pattern**: Different implementations for CSV writing, file system operations, and scheduling
- **Factory Pattern**: Configuration service for creating application settings
- **Observer Pattern**: Background service for scheduled operations
- **Separation of Concerns**: Clean architecture with dedicated scheduling logic

For detailed architectural information, see [ARCHITECTURE.md](ARCHITECTURE.md).

For method-level requirements fulfillment documentation, see [METHODS_DOCUMENTATION.md](METHODS_DOCUMENTATION.md).

## Configuration

### Command Line Arguments

- `--output-folder` or `-o`: Output folder for CSV files
- `--interval` or `-i`: Extract interval in minutes
- `--timezone` or `-t`: Time zone ID
- `--help` or `-h`: Show help message (note: currently displays help but doesn't exit)

### Configuration File (appsettings.json)

The application uses a configuration service that loads settings from `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "OutputFolderPath": "Output",
  "ExtractIntervalMinutes": 15,
  "TimeZoneId": "Europe/Berlin"
}
```

**Note**: Command line arguments take precedence over configuration file settings.

**Validation**: `ExtractIntervalMinutes` > 0, required `OutputFolderPath`, valid `TimeZoneId`

## Requirements Fulfillment

This application fully implements all requirements from the specification:

✅ **Console Application**: .NET 9 console application using C#  
✅ **CSV Format**: Two columns (Datetime, Volume) with semicolon separator and point decimal separator  
✅ **Hourly Aggregation**: All trade positions aggregated per hour  
✅ **UTC Datetime**: Datetime column in UTC with ISO 8601 format  
✅ **DST Handling**: Proper handling of Daylight Saving Time transitions  
✅ **Filename Convention**: PowerPosition_YYYYMMDD_YYYYMMDDHHMM.csv format  
✅ **Configuration**: Command line and configuration file support  
✅ **Scheduled Execution**: Configurable interval with +/- 1 minute tolerance  
✅ **Retry Mechanism**: Built-in retry logic to prevent missed extractions  
✅ **Initial Extract**: Runs immediately on startup  
✅ **Production Logging**: Comprehensive logging for production support  
✅ **PowerService Integration**: Uses provided PowerService.dll assembly  
✅ **Day-Ahead Logic**: Requests data for the following day  
✅ **Timezone Independence**: Consistent output regardless of server location  
✅ **Centralized Scheduling**: Dedicated `ScheduleCalculator` class  
✅ **Configuration Validation**: Property-level validation with CommandLineParser  

## Usage

### Basic Usage

```bash
# Run with default settings (15-minute intervals, Output folder)
PowerTradePosition.Console.exe

# Custom output folder
PowerTradePosition.Console.exe --output-folder C:\Reports

# Custom interval (30 minutes)
PowerTradePosition.Console.exe --interval 30

# Custom timezone
PowerTradePosition.Console.exe --timezone Europe/London

# Combined options
PowerTradePosition.Console.exe --output-folder C:\Reports --interval 60 --timezone Europe/Paris
```

**Note**: The `--help` flag will display help information and exit the application. To stop the application during normal operation, use Ctrl+C.

### CSV Output Format

The application generates CSV files with the naming convention:
`PowerPosition_YYYYMMDD_YYYYMMDDHHMM.csv`

Example: `PowerPosition_20230702_202307011915.csv`

CSV content format:
```csv
Datetime;Volume
2023-07-01T22:00:00Z;150
2023-07-01T23:00:00Z;150
2023-07-02T00:00:00Z;150
...
```

### Example from Specification

The application correctly handles the example scenario from the specification:

**Input**: Two trade positions for 2023-07-02 with different volumes per period  
**Extraction Time**: 2023-07-01T21:15:00 (Europe/Madrid)  
**Output File**: `PowerPosition_20230702_202307011915.csv`  
**Result**: 24 hourly positions with aggregated volumes (e.g., 150 for hours 0-9, 80 for hours 10-23)

The application automatically:
- Requests data for the day-ahead (2023-07-02)
- Converts Europe/Berlin timezone to UTC
- Aggregates volumes from multiple trades
- Handles DST transitions correctly
- Generates the exact filename format specified

## Building and Running

### Prerequisites

1. Install .NET 9.0 SDK
2. Ensure PowerService.dll is in the lib folder

### Build

```bash
dotnet build PowerTradePosition.sln
```

### Run

```bash
dotnet run --project PowerTradePosition.Console
```

### Publish

```bash
dotnet publish PowerTradePosition.Console -c Release -o ./publish
```

## Key Components

### PositionExtractor
Orchestrates the extraction process with retry logic and error handling.

### ScheduledExtractor
Background service that manages scheduled executions with timing tolerance.

### ScheduleCalculator
Dedicated class that handles scheduling logic: calculating intervals, delays, and timezone configurations.

### PositionAggregator
Aggregates power trades by hour, handling timezone conversions and period mapping.

### TimeGridBuilder
Creates hourly time grids considering timezone and daylight saving time.

### CsvWriter
Generates CSV files with proper formatting and naming conventions.

### PowerServiceWrapper
Wraps the external PowerService.dll using reflection for dynamic loading.

### CommandLineParser
Configuration service using CommandLineParser NuGet package for argument parsing and file loading.

### ApplicationConfiguration
Configuration class with property-level validation for `ExtractIntervalMinutes` (must be > 0).

## Error Handling

- **Retry Logic**: Built-in retry mechanism with exponential backoff
- **Logging**: Comprehensive logging at all levels for debugging
- **Graceful Degradation**: Continues operation even if individual extractions fail
- **Configuration Validation**: Early detection and clear error messages

## Timezone Handling

Handles Europe/Berlin timezone with DST transitions, UTC conversion, and server location independence.

## Testing Considerations

Designed for testability with interface abstractions, mockable services, externalized configuration, and separated business logic.

## Production Deployment

Configure appropriate log levels, environment-specific configuration files, and monitor application logs and CSV generation.

## Troubleshooting

### Common Issues

1. **PowerService.dll not found**: Ensure the DLL is in the lib folder
2. **Timezone errors**: Verify the timezone ID is valid for the target system
3. **Permission errors**: Ensure the application has write access to the output folder
4. **Scheduling issues**: Check the interval configuration and system time
5. **Configuration validation errors**: Check that ExtractIntervalMinutes is greater than 0 and timezone is valid

### Log Analysis

Provides detailed logging for configuration loading, trade retrieval, position aggregation, file operations, scheduling, and error conditions.

## Known Limitations

1. **Help Command**: The `--help` flag displays help information and exits the application
2. **Configuration Precedence**: Command line arguments override configuration file settings
3. **Single Instance**: The application is designed to run as a single instance

## Architecture Highlights

- **Scheduling Logic**: Dedicated `ScheduleCalculator` class for better separation of concerns and testability
- **Configuration Validation**: Property-level validation with early error detection and clear messages
- **Enhanced Testability**: Independent testing with mocked dependencies for better coverage

## License

This project is part of a development challenge and is provided as-is.

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
- **Centralized Scheduling**: Dedicated `ScheduleCalculator` class for better separation of concerns
- **Configuration Validation**: Early validation of configuration values at the property level

## Requirements

- .NET 9.0 or higher
- Windows (due to PowerService.dll dependency)
- PowerService.dll assembly (provided in lib folder)

## Project Structure

```
PowerTradePosition/
├── PowerTradePosition.Console/          # Console application entry point
├── PowerTradePosition.Domain/           # Domain models and business logic
│   ├── Domain/                         # Domain entities and services
│   └── Interfaces/                     # Domain interfaces
├── PowerTradePosition.DataAccess/       # Data access layer
└── lib/                                # External dependencies (PowerService.dll)
```

## Architecture

The solution follows SOLID principles and implements several design patterns:

- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection
- **Repository Pattern**: ITradeService interface for data access
- **Strategy Pattern**: Different implementations for CSV writing, file system operations, and scheduling
- **Factory Pattern**: Configuration service for creating application settings
- **Observer Pattern**: Background service for scheduled operations
- **Separation of Concerns**: Scheduling logic extracted to dedicated `ScheduleCalculator` class

For detailed architectural information, see [ARCHITECTURE.md](ARCHITECTURE.md).

For method-level requirements fulfillment documentation, see [METHODS_DOCUMENTATION.md](METHODS_DOCUMENTATION.md).

## Configuration

### Command Line Arguments

- `--output-folder` or `-o`: Output folder for CSV files
- `--interval` or `-i`: Extract interval in minutes
- `--timezone` or `-t`: Time zone ID
- `--help` or `-h`: Show help message (note: currently displays help but doesn't exit)

### Configuration File (appsettings.json)

The application uses a custom configuration service that loads settings from `appsettings.json`:

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
  "TimeZoneId": "Europe/Berlin",
  "RetryAttempts": 3,
  "RetryDelaySeconds": 30
}
```

**Note**: Command line arguments take precedence over configuration file settings.

All numeric configuration values are validated at the property level:
- `ExtractIntervalMinutes` must be greater than 0
- `RetryAttempts` must be greater than 0
- `RetryDelaySeconds` must be greater than 0

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
✅ **Centralized Scheduling**: Scheduling logic extracted to dedicated class  
✅ **Configuration Validation**: Early validation of configuration values  

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
Background service that manages scheduled executions with timing tolerance. Focuses on background service orchestration while delegating scheduling calculations to `ScheduleCalculator`.

### ScheduleCalculator
Dedicated class that handles all scheduling logic, including:
- Calculating next execution intervals
- Determining delays until next execution
- Handling timezone and interval configurations

### PositionAggregator
Aggregates power trades by hour, handling timezone conversions and period mapping.

### TimeGridBuilder
Creates hourly time grids considering timezone and daylight saving time.

### CsvWriter
Generates CSV files with proper formatting and naming conventions.

### PowerServiceWrapper
Wraps the external PowerService.dll using reflection for dynamic loading.

### CommandLineParser
Custom configuration service that handles command line arguments and configuration file loading with proper precedence.

### ApplicationConfiguration
Enhanced configuration class with property-level validation:
- All numeric properties validate values at the setter level
- Clear error messages for invalid configuration
- Prevents invalid configuration from propagating through the system

## Error Handling

- **Retry Logic**: Configurable retry attempts with exponential backoff
- **Logging**: Comprehensive logging at all levels for debugging
- **Graceful Degradation**: Continues operation even if individual extractions fail
- **Exception Handling**: Proper exception handling throughout the application
- **Configuration Validation**: Early detection and clear error messages for invalid configuration

## Timezone Handling

The application properly handles:
- Europe/Berlin timezone (as specified in requirements)
- Daylight Saving Time transitions
- UTC conversion for output consistency
- Server location independence

## Testing Considerations

The solution is designed to be testable:
- All dependencies are abstracted through interfaces
- Services can be easily mocked for unit testing
- Configuration is externalized and injectable
- Business logic is separated from infrastructure concerns
- **Scheduling Logic**: Can be tested independently with mocked time and configuration
- **Configuration Validation**: Comprehensive test coverage for validation logic

## Production Deployment

- **Logging**: Configure appropriate log levels for production
- **Configuration**: Use environment-specific configuration files
- **Monitoring**: Monitor application logs and CSV file generation
- **Error Handling**: Configure appropriate retry policies and alerting

## Troubleshooting

### Common Issues

1. **PowerService.dll not found**: Ensure the DLL is in the lib folder
2. **Timezone errors**: Verify the timezone ID is valid for the target system
3. **Permission errors**: Ensure the application has write access to the output folder
4. **Scheduling issues**: Check the interval configuration and system time
5. **Configuration validation errors**: Check that all numeric configuration values are greater than 0

### Log Analysis

The application provides detailed logging for:
- Configuration loading
- Trade retrieval
- Position aggregation
- File operations
- Scheduling operations
- Error conditions
- Configuration validation failures

## Known Limitations

1. **Help Command**: The `--help` flag displays help information and exits the application
2. **Configuration Precedence**: Command line arguments override configuration file settings, but the merge logic is custom-implemented
3. **Single Instance**: The application is designed to run as a single instance

## Architecture Highlights

### Scheduling Logic Design
- **Implementation**: Dedicated `ScheduleCalculator` class handles all scheduling calculations
- **Benefits**: Better separation of concerns, easier testing, more focused classes

### Configuration Validation
- **Implementation**: Property-level validation prevents invalid values from being set
- **Benefits**: Early error detection, clearer error messages, better data integrity

### Enhanced Testability
- **Implementation**: `ScheduleCalculator` can be tested independently with mocked dependencies
- **Benefits**: Better test coverage, easier debugging, more maintainable code

## License

This project is part of a development challenge and is provided as-is.

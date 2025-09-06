# Power Trade Position - Methods Documentation

This document explains how each method in the project fulfills the requirements from the specification.

## Core Requirements Fulfillment

### 1. Console Application with .NET 9+
- **File**: `PowerTradePosition.Console/Program.cs`
- **Method**: `Main(string[] args)`
- **Fulfillment**: Uses .NET 9 with console application structure using `Host.CreateDefaultBuilder` and `UseConsoleLifetime()`

### 2. CSV Format Requirements

#### 2a. Two Columns: Datetime and Volume
- **File**: `PowerTradePosition.Domain/Domain/CsvWriter.cs`
- **Method**: `WriteToFileAsync(IEnumerable<PowerPosition> positions, string filePath, CancellationToken ct)`
- **Fulfillment**: Creates header "Datetime;Volume" and formats each row with datetime and volume values

#### 2b. First Row as Header
- **Fulfillment**: First line in CSV is "Datetime;Volume"

#### 2c. Semicolon as Separator
- **Fulfillment**: Uses semicolon (;) as separator in header and data rows

#### 2d. Point as Decimal Separator
- **Fulfillment**: Uses `CultureInfo.InvariantCulture` to ensure point (.) as decimal separator

#### 2e. Hourly Aggregation
- **File**: `PowerTradePosition.Domain/Domain/PositionAggregator.cs`
- **Method**: `AggregatePositionsByHour(IEnumerable<PowerTrade> trades, string timeZoneId)`
- **Fulfillment**: Aggregates all trade volumes by hour using period numbers (1-24) mapped to hourly slots

#### 2f. UTC Datetime Column
- **File**: `PowerTradePosition.Domain/Domain/TimeGridBuilder.cs`
- **Method**: `BuildHourlyTimeGrid(DateTime date, string timeZoneId)`
- **Fulfillment**: Converts local timezone (Europe/Berlin) to UTC for all datetime values

#### 2g. ISO 8601 Format
- **File**: `PowerTradePosition.Domain/Domain/CsvWriter.cs`
- **Method**: `WriteToFileAsync(...)`
- **Fulfillment**: Formats datetime as "yyyy-MM-ddTHH:mm:ssZ" (ISO 8601 UTC format)

#### 2h. Daylight Saving Time Handling
- **File**: `PowerTradePosition.Domain/Domain/TimeGridBuilder.cs`
- **Method**: `BuildHourlyTimeGrid(DateTime date, string timeZoneId)`
- **Fulfillment**: Handles DST transitions gracefully by catching `ArgumentException` during invalid times and adjusting

### 3. CSV Filename Convention
- **File**: `PowerTradePosition.Domain/Domain/CsvWriter.cs`
- **Method**: `GenerateFileName(DateTime dayAheadDate, DateTime extractionTime)`
- **Fulfillment**: Creates filename in format `PowerPosition_YYYYMMDD_YYYYMMDDHHMM.csv` where:
  - First date: day-ahead date (volumes)
  - Second date: extraction timestamp in UTC

### 4. Configuration Options
- **File**: `PowerTradePosition.Console/CommandLineParser.cs`
- **Method**: `ParseConfiguration(string[] args)`
- **Fulfillment**: Supports both command line arguments and configuration file (`appsettings.json`) with command line taking precedence

**Implementation Details**:
- Custom command line argument parsing (not using Microsoft.Extensions.Configuration.CommandLine)
- Manual configuration merging logic
- Support for `--output-folder`, `--interval`, `--timezone`, and `--help` flags
- Configuration validation at the property level in `ApplicationConfiguration`

### 5. Scheduled Execution
- **File**: `PowerTradePosition.Console/ScheduledExtractor.cs`
- **Method**: `ExecuteAsync(CancellationToken stoppingToken)`
- **Fulfillment**: Runs at configurable intervals with tolerance of +/- 1 minute using `ScheduleCalculator.CalculateDelayUntilNextExecution()` method

Scheduling logic has been extracted to a dedicated `ScheduleCalculator` class for better separation of concerns and testability.

### 6. Retry Mechanism
- **File**: `PowerTradePosition.Domain/Domain/PositionExtractor.cs`
- **Method**: `ExtractPositionsAsync(CancellationToken ct)`
- **Fulfillment**: Implements retry logic with configurable attempts and exponential backoff delay

### 7. Initial Extract on Startup
- **File**: `PowerTradePosition.Console/ScheduledExtractor.cs`
- **Method**: `RunInitialExtractionAsync(CancellationToken stoppingToken)`
- **Fulfillment**: Runs extraction immediately when application starts before scheduling subsequent extractions

### 8. Production Logging
- **File**: Multiple files throughout the project
- **Fulfillment**: Comprehensive logging using `ILogger<T>` with appropriate log levels (Information, Warning, Error, Critical) for production diagnostics

## Additional Requirements Fulfillment

### PowerService Integration
- **File**: `PowerTradePosition.DataAccess/PowerServiceWrapper.cs`
- **Method**: `GetTradesAsync(DateTime dayAheadLocalDate, CancellationToken ct)`
- **Fulfillment**: Wraps the provided `PowerService.dll` assembly, using async method and converting to domain models

### Day-Ahead Date Logic
- **File**: `PowerTradePosition.Domain/Domain/PositionExtractor.cs`
- **Method**: `PerformExtractionAsync(CancellationToken ct)`
- **Fulfillment**: Requests data for `DateTime.Today.AddDays(1)` to get day-ahead positions

### Timezone Configuration
- **File**: `PowerTradePosition.Domain/Domain/Configuration.cs`
- **Fulfillment**: Default timezone set to "Europe/Berlin" as specified, configurable via command line or config file

### Server Location Independence
- **File**: `PowerTradePosition.Domain/Domain/TimeGridBuilder.cs`
- **Method**: `BuildHourlyTimeGrid(DateTime date, string timeZoneId)`
- **Fulfillment**: Explicitly converts to UTC regardless of server location, ensuring consistent output

## ScheduleCalculator Class

### Purpose
The `ScheduleCalculator` class centralizes all scheduling logic that was previously embedded in the `ScheduledExtractor` class.

### Key Methods

#### `CalculateNextInterval()`
- **Purpose**: Calculates the next scheduled execution time based on the current time and configured interval
- **Dependencies**: Uses `ApplicationConfiguration.ExtractIntervalMinutes` and `TimeProvider.GetUtcNow()`
- **Logic**: Rounds down to the nearest interval boundary and handles end-of-day transitions

#### `CalculateDelayUntilNextExecution()`
- **Purpose**: Calculates the time delay until the next scheduled execution
- **Dependencies**: Calls `CalculateNextInterval()` and compares with current time
- **Returns**: `TimeSpan` representing the delay until next execution

### Benefits
- **Separation of Concerns**: Scheduling logic is separate from background service orchestration
- **Testability**: Can be unit tested independently with mocked time and configuration
- **Reusability**: Can be used by other components that need scheduling calculations
- **Maintainability**: Scheduling logic is centralized and easier to modify

## Method Details

### PositionExtractor.ExtractPositionsAsync()
- **Purpose**: Main orchestration method for position extraction
- **Retry Logic**: Implements exponential backoff retry mechanism
- **Error Handling**: Comprehensive logging and exception handling
- **Requirements Met**: 6, 7, 8

### PositionAggregator.AggregatePositionsByHour()
- **Purpose**: Aggregates trade volumes by hour
- **Period Mapping**: Maps period numbers 1-24 to hourly slots (0-23)
- **Requirements Met**: 2e

### TimeGridBuilder.BuildHourlyTimeGrid()
- **Purpose**: Creates 24-hour time grid with proper timezone conversion
- **DST Handling**: Gracefully handles daylight saving time transitions
- **UTC Conversion**: Ensures all times are in UTC
- **Requirements Met**: 2f, 2h

### CsvWriter.WriteToFileAsync()
- **Purpose**: Writes aggregated positions to CSV file
- **Format Compliance**: Implements all CSV format requirements
- **Requirements Met**: 2a, 2b, 2c, 2d, 2g

### ScheduledExtractor.ExecuteAsync()
- **Purpose**: Manages scheduled execution of extractions
- **Timing**: Uses `ScheduleCalculator` to determine next execution time
- **Initial Run**: Executes extraction immediately on startup
- **Requirements Met**: 5, 7

### ScheduleCalculator.CalculateDelayUntilNextExecution()
- **Purpose**: Calculates the delay until the next scheduled execution
- **Dependencies**: Uses `ApplicationConfiguration.ExtractIntervalMinutes` and `TimeProvider`
- **Logic**: Determines next interval boundary and calculates time difference
- **Requirements Met**: 5 (scheduling logic)

### CommandLineParser.ParseConfiguration()
- **Purpose**: Loads and merges configuration from multiple sources
- **Priority**: Command line arguments override configuration file
- **Validation**: Ensures required configuration is available
- **Requirements Met**: 4

**Implementation Notes**:
- Custom command line parsing logic
- Manual configuration merging
- Help text display and application exit

### ApplicationConfiguration Property Validation
- **Purpose**: Validates configuration values at the setter level
- **Properties**: `ExtractIntervalMinutes`, `RetryAttempts`, `RetryDelaySeconds`
- **Validation**: All numeric values must be greater than 0
- **Benefits**: Early validation prevents invalid configuration from propagating

## Current Implementation Status

### Fully Implemented ✅
- All core CSV format requirements
- Timezone handling with DST support
- Retry mechanism with exponential backoff
- Scheduled execution with timing tolerance
- Initial extraction on startup
- Production logging
- PowerService integration
- Day-ahead date logic
- Centralized scheduling logic in `ScheduleCalculator`
- Configuration validation at the property level

### Partially Implemented ⚠️
- **Configuration**: Custom implementation instead of standard Microsoft patterns

### Implementation Details

#### Configuration Service
- **Custom Parser**: Manual command line argument parsing in `CommandLineParser`
- **Configuration Merging**: Custom logic for merging command line and file settings
- **Help Display**: Shows help text and exits the application
- Property-level validation in `ApplicationConfiguration`

#### Command Line Support
- **Supported Flags**: `--output-folder` (`-o`), `--interval` (`-i`), `--timezone` (`-t`), `--help` (`-h`)
- **Parsing Logic**: Custom switch statement in `ParseCommandLineArgs` method
- **Validation**: Validation happens at the configuration property level

#### Configuration File
- **Format**: Standard `appsettings.json` with custom configuration keys
- **Keys**: `OutputFolderPath`, `ExtractIntervalMinutes`, `TimeZoneId`, `RetryAttempts`, `RetryDelaySeconds`
- **Logging**: Separate logging configuration section

## Testing Coverage

### Unit Tests Available
- **ExtractorTests.cs**: Tests for position extraction logic
- **AggregatorTests.cs**: Tests for position aggregation
- **ConfigurationServiceTests.cs**: Tests for configuration loading
- **ScheduledExtractorTests.cs**: Tests for scheduled execution
- **ScheduleCalculatorTests.cs**: Tests for scheduling logic
- **ConfigurationTests.cs**: Tests for configuration validation

### Test Scenarios Covered
- CSV format compliance
- Timezone conversion accuracy
- DST transition handling
- Configuration precedence
- Retry mechanism behavior
- Scheduled execution timing
- Scheduling calculation accuracy
- Configuration validation behavior

## Future Enhancement Opportunities

### Configuration Improvements
- Implement standard Microsoft configuration patterns
- Add configuration validation
- Support for environment variables
- Configuration hot-reload capability

### Help Command Enhancement
- Add more detailed usage examples
- Include configuration file format documentation

### Additional Command Line Options
- Add `--version` flag
- Support for configuration file path specification
- Verbose logging option
- Dry-run mode for testing

### Scheduling Enhancements
- Support for cron-like expressions
- Multiple scheduling strategies
- Dynamic interval adjustment based on load
- Scheduling conflict resolution

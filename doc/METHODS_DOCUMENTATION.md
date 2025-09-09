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

**Implementation**: CommandLineParser package with automatic merging, property validation for `ExtractIntervalMinutes`

### 5. Scheduled Execution
- **File**: `PowerTradePosition.Console/ScheduledExtractor.cs`
- **Method**: `ExecuteAsync(CancellationToken stoppingToken)`
- **Fulfillment**: Runs at configurable intervals with tolerance of +/- 1 minute using `ScheduleCalculator.CalculateDelayUntilNextExecution()` method

Uses dedicated `ScheduleCalculator` class for scheduling logic.

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

Centralizes scheduling logic with key methods:
- `CalculateNextInterval()`: Determines next execution time based on interval configuration
- `CalculateDelayUntilNextExecution()`: Calculates delay until next execution

**Benefits**: Separation of concerns, testability, centralized scheduling logic

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

**Implementation**: CommandLineParser package with automatic merging, property validation

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
- Configuration management with CommandLineParser package and property validation

## Testing Coverage

### Unit Tests Available
- **ExtractorTests.cs**: Tests for position extraction logic
- **AggregatorTests.cs**: Tests for position aggregation
- **ConfigurationServiceTests.cs**: Tests for configuration loading
- **ScheduledExtractorTests.cs**: Tests for scheduled execution
- **ScheduleCalculatorTests.cs**: Tests for scheduling logic
- **ConfigurationTests.cs**: Tests for configuration validation

### Test Scenarios Covered
- CSV format compliance, timezone conversion, DST handling
- Configuration precedence and validation
- Retry mechanism and scheduled execution timing
- Scheduling calculation accuracy

## Future Enhancement Opportunities

### Configuration & Command Line
- Environment variables support, configuration hot-reload
- Additional command line options (`--version`, verbose logging, dry-run mode)
- Enhanced help documentation

### Scheduling & Performance
- Cron-like expressions, multiple scheduling strategies
- Dynamic interval adjustment, scheduling conflict resolution

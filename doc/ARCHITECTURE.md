# Power Trade Position - Architecture Document

## Overview

The Power Trade Position application is designed as a robust, production-ready console application that extracts and aggregates power trade positions on a scheduled basis. The architecture follows clean architecture principles with clear separation of concerns and dependency injection.

## Architectural Decisions

### 1. Clean Architecture Pattern

The application follows Clean Architecture principles with three main layers:

- **Console Layer** (`PowerTradePosition.Console`): Application entry point and scheduling
- **Domain Layer** (`PowerTradePosition.Domain`): Business logic and domain models
- **Data Access Layer** (`PowerTradePosition.DataAccess`): External service integration

**Rationale**: This separation ensures that business logic is independent of external dependencies, making the system testable and maintainable.

### 2. Dependency Injection

**Decision**: Use Microsoft's built-in DI container with service lifetime management.

**Implementation**: 
- Scoped services for business logic components
- Singleton for configuration service
- Hosted service for background scheduling

**Benefits**: 
- Easier testing through interface mocking
- Loose coupling between components
- Centralized service management

### 3. Background Service Pattern

**Decision**: Use `BackgroundService` for scheduled execution instead of `Timer` or `Task.Delay`.

**Implementation**: `ScheduledExtractor` inherits from `BackgroundService` with proper cancellation token support.

**Benefits**:
- Graceful shutdown handling
- Integration with .NET Host lifecycle
- Better resource management

### 4. Interface Segregation

**Decision**: Define focused interfaces for each responsibility.

**Examples**:
- `IPositionExtractor`: Core extraction logic
- `ITradeService`: Data retrieval abstraction
- `ICsvWriter`: File output abstraction
- `IFileSystem`: File system operations
- `IScheduleCalculator`: Scheduling calculation abstraction

**Benefits**: 
- Easier testing and mocking
- Clear contract definitions
- Loose coupling between components

### 5. Configuration Management

**Decision**: Custom configuration service with multi-source configuration and precedence hierarchy.

**Implementation**:
1. Command line arguments (highest priority)
2. Configuration file (`appsettings.json`)
3. Default values

**Current Implementation Details**:
- Custom `CommandLineParser` class that parses command line arguments manually
- Direct configuration file loading from `IConfiguration`
- Manual merging of configurations with command line precedence
- `ApplicationConfiguration` class with property-level validation

**Benefits**:
- Flexible deployment options
- Environment-specific configuration
- Override capabilities for testing
- Full control over configuration parsing logic
- Early validation of configuration values at the source

### 6. Error Handling and Retry Logic

**Decision**: Implement retry mechanism with exponential backoff.

**Implementation**: 
- Configurable retry attempts and delays
- Exponential backoff strategy
- Comprehensive logging at all levels

**Benefits**:
- Resilience against transient failures
- Configurable retry behavior
- Production-ready error handling

### 7. Timezone Handling

**Decision**: Explicit timezone conversion to UTC with DST awareness.

**Implementation**: 
- Use `TimeZoneInfo` for accurate conversions
- Handle DST transition edge cases
- Server location independence

**Benefits**:
- Consistent output regardless of server location
- Accurate time calculations
- Compliance with specification requirements

### 8. Scheduling Logic Separation

**Decision**: Extract scheduling calculation logic into a separate, testable class.

**Implementation**: 
- `IScheduleCalculator` interface defines scheduling contract
- `ScheduleCalculator` class implements scheduling logic
- `ScheduledExtractor` focuses on background service orchestration
- Configuration and time provider dependencies moved to `ScheduleCalculator`

**Benefits**:
- Better separation of concerns
- Easier unit testing of scheduling logic
- More focused `ScheduledExtractor` class
- Centralized scheduling dependencies

## Design Patterns Used

### 1. Adapter Pattern
**Location**: `PowerServiceWrapper`
**Purpose**: Adapts external `PowerService.dll` to internal domain interfaces.

### 2. Strategy Pattern
**Location**: Various interfaces (`ICsvWriter`, `IFileSystem`, `IScheduleCalculator`, etc.)
**Purpose**: Allows different implementations for file operations, data output, and scheduling.

### 3. Factory Pattern
**Location**: `TimeGridBuilder`
**Purpose**: Creates time grids with proper timezone handling.

### 4. Template Method Pattern
**Location**: `BackgroundService` base class
**Purpose**: Defines the execution flow while allowing customization.

### 5. Repository Pattern
**Location**: `ITradeService` interface
**Purpose**: Abstracts data access from business logic.

### 6. Custom Configuration Pattern
**Location**: `CommandLineParser`
**Purpose**: Provides custom configuration parsing and merging logic.

### 7. Validation Pattern
**Location**: `ApplicationConfiguration` properties
**Purpose**: Validates configuration values at the setter level, ensuring data integrity.

## Data Flow Architecture

```
[Console] → [ScheduledExtractor] → [ScheduleCalculator] → [PositionExtractor] → [PositionAggregator]
                                                      ↓
[PowerService.dll] ← [PowerServiceWrapper] ← [ITradeService]
                                                      ↓
                                              [TimeGridBuilder] → [CsvWriter] → [File System]
```

## Configuration Architecture

```
Command Line Args → CommandLineParser → ApplicationConfiguration
       ↑                    ↓
       └─── Override ──── appsettings.json
```

**Current Implementation**:
- `CommandLineParser` manually parses command line arguments
- Configuration file loaded through `IConfiguration` dependency
- Manual merging with command line taking precedence
- `ApplicationConfiguration` validates all numeric properties (must be > 0)

## Security Considerations

### Current Implementation
- No authentication/authorization (not required by specification)
- File system access limited to configured output directory
- No sensitive data exposure in logs

### Potential Enhancements
- File access permissions validation
- Output directory path validation
- Audit logging for file operations

## Performance Characteristics

### Current Implementation
- **Latency**: Minimal - processes data in memory
- **Throughput**: Limited by external service response time
- **Resource Usage**: Low memory footprint, single-threaded processing

### Scalability Considerations
- **Horizontal**: Multiple instances can run independently
- **Vertical**: Limited by single-threaded design
- **Bottlenecks**: External service calls and file I/O

## Alternative Architectural Approaches

### 1. Microservices Architecture

**Scenario**: If the application needs to scale across multiple services.

**Implementation**:
```
[API Gateway] → [Extraction Service] → [Aggregation Service] → [File Service]
                     ↓
              [Message Queue] → [Multiple Workers]
```

**Benefits**: Independent scaling, fault isolation
**Drawbacks**: Increased complexity, network overhead

### 2. Event-Driven Architecture

**Scenario**: If real-time updates and multiple consumers are needed.

**Implementation**:
```
[Power Service] → [Event Bus] → [Position Extractor] → [Multiple Consumers]
                                    ↓
                              [Event Store] → [Analytics]
```

**Benefits**: Loose coupling, real-time processing
**Drawbacks**: Event ordering challenges, eventual consistency

### 3. CQRS Pattern

**Scenario**: If read and write operations have different performance requirements.

**Implementation**:
```
[Command Side] → [Event Store] → [Read Side] → [Optimized Queries]
```

**Benefits**: Optimized read performance, scalability
**Drawbacks**: Data consistency challenges, complexity

## Latency Optimization Scenarios

### 1. High-Frequency Trading Requirements

**Current Limitation**: Single-threaded processing
**Optimization**: Parallel processing of trades

```csharp
public async Task<IEnumerable<PowerPosition>> AggregatePositionsByHourParallel(
    IEnumerable<PowerTrade> trades, string timeZoneId)
{
    var tradeArray = trades.ToArray();
    var timeGrid = timeGridBuilder.BuildHourlyTimeGrid(dayAheadDate, timeZoneId).ToList();
    
    var hourlyVolumes = new ConcurrentDictionary<DateTime, double>();
    
    await Parallel.ForEachAsync(tradeArray, async (trade, ct) =>
    {
        foreach (var period in trade.Periods)
        {
            var hourIndex = period.Period - 1;
            if (hourIndex >= timeGrid.Count) continue;
            var hourTime = timeGrid[hourIndex];
            hourlyVolumes.AddOrUpdate(hourTime, period.Volume, (_, existing) => existing + period.Volume);
        }
    });
    
    return hourlyVolumes.Select(kvp => new PowerPosition(kvp.Key, kvp.Value))
                       .OrderBy(p => p.DateTime);
}
```

### 2. Real-Time Processing

**Current Limitation**: Batch processing
**Optimization**: Streaming with backpressure

```csharp
public async IAsyncEnumerable<PowerPosition> StreamPositionsAsync(
    IAsyncEnumerable<PowerTrade> trades, string timeZoneId)
{
    var timeGrid = timeGridBuilder.BuildHourlyTimeGrid(dayAheadDate, timeZoneId).ToList();
    var hourlyVolumes = new Dictionary<DateTime, double>();
    
    await foreach (var trade in trades)
    {
        foreach (var period in trade.Periods)
        {
            // Process and yield results as they become available
            var position = ProcessPeriod(period, timeGrid, hourlyVolumes);
            if (position != null)
                yield return position;
        }
    }
}
```

## Security Enhancement Scenarios

### 1. Multi-Tenant Environment

**Current Limitation**: Single configuration
**Enhancement**: Tenant isolation and access control

```csharp
public interface ITenantService
{
    Task<TenantConfiguration> GetTenantConfigAsync(string tenantId);
    Task<bool> ValidateAccessAsync(string tenantId, string resource);
}

public class SecurePositionExtractor
{
    private readonly ITenantService _tenantService;
    
    public async Task ExtractPositionsAsync(string tenantId, CancellationToken ct)
    {
        var tenantConfig = await _tenantService.GetTenantConfigAsync(tenantId);
        if (!await _tenantService.ValidateAccessAsync(tenantId, "extract_positions"))
            throw new UnauthorizedAccessException();
            
        // Use tenant-specific configuration
        await ExtractWithConfigAsync(tenantConfig, ct);
    }
}
```

### 2. Data Encryption

**Current Limitation**: Plain text CSV output
**Enhancement**: Encrypted file storage

```csharp
public interface IEncryptionService
{
    Task<byte[]> EncryptAsync(byte[] data, string keyId);
    Task<byte[]> DecryptAsync(byte[] encryptedData, string keyId);
}

public class SecureCsvWriter : ICsvWriter
{
    private readonly IEncryptionService _encryptionService;
    
    public async Task WriteToFileAsync(IEnumerable<PowerPosition> positions, string filePath, CancellationToken ct)
    {
        var csvContent = GenerateCsvContent(positions);
        var encryptedContent = await _encryptionService.EncryptAsync(
            Encoding.UTF8.GetBytes(csvContent), "csv_encryption_key");
        
        await File.WriteAllBytesAsync(filePath, encryptedContent, ct);
    }
}
```

## Scalability Enhancement Scenarios

### 1. Horizontal Scaling

**Current Limitation**: Single instance
**Enhancement**: Distributed processing with coordination

```csharp
public interface IDistributedCoordinator
{
    Task<bool> AcquireLockAsync(string lockKey, TimeSpan timeout);
    Task ReleaseLockAsync(string lockKey);
    Task<IEnumerable<string>> GetActiveWorkersAsync();
}

public class DistributedExtractor : IPositionExtractor
{
    private readonly IDistributedCoordinator _coordinator;
    
    public async Task ExtractPositionsAsync(CancellationToken ct)
    {
        var lockKey = $"extraction_{DateTime.UtcNow:yyyyMMdd}";
        
        if (!await _coordinator.AcquireLockAsync(lockKey, TimeSpan.FromMinutes(5)))
        {
            _logger.LogInformation("Another worker is already processing this extraction");
            return;
        }
        
        try
        {
            await PerformExtractionAsync(ct);
        }
        finally
        {
            await _coordinator.ReleaseLockAsync(lockKey);
        }
    }
}
```

### 2. Database Persistence

**Current Limitation**: File-based storage only
**Enhancement**: Hybrid storage with database

```csharp
public interface IPositionRepository
{
    Task SavePositionsAsync(IEnumerable<PowerPosition> positions, DateTime extractionTime);
    Task<IEnumerable<PowerPosition>> GetPositionsAsync(DateTime date);
    Task<IEnumerable<ExtractionHistory>> GetExtractionHistoryAsync(DateTime from, DateTime to);
}

public class HybridStorageExtractor : IPositionExtractor
{
    private readonly IPositionRepository _repository;
    
    public async Task ExtractPositionsAsync(CancellationToken ct)
    {
        var positions = await ExtractPositionsFromServiceAsync(ct);
        
        // Save to database for analytics and audit
        await _repository.SavePositionsAsync(positions, DateTime.UtcNow);
        
        // Also save to CSV for compliance
        await _csvWriter.WriteToFileAsync(positions, filePath, ct);
    }
}
```

## Monitoring and Observability

### Current Implementation
- Structured logging with different levels
- Basic error tracking
- Performance timing for extractions

### Enhancement Scenarios

#### 1. Metrics Collection
```csharp
public interface IMetricsCollector
{
    void RecordExtractionDuration(TimeSpan duration);
    void RecordTradeCount(int count);
    void RecordError(string errorType);
}

public class InstrumentedExtractor : IPositionExtractor
{
    private readonly IMetricsCollector _metrics;
    
    public async Task ExtractPositionsAsync(CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var trades = await _tradeService.GetTradesAsync(dayAheadDate, ct);
            _metrics.RecordTradeCount(trades.Count());
            
            await PerformExtractionAsync(trades, ct);
        }
        catch (Exception ex)
        {
            _metrics.RecordError(ex.GetType().Name);
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _metrics.RecordExtractionDuration(stopwatch.Elapsed);
        }
    }
}
```

#### 2. Distributed Tracing
```csharp
public class TracedExtractor : IPositionExtractor
{
    private readonly ILogger<PositionExtractor> _logger;
    
    public async Task ExtractPositionsAsync(CancellationToken ct)
    {
        using var activity = ActivitySource.StartActivity("ExtractPositions");
        activity?.SetTag("day_ahead_date", dayAheadDate.ToString("yyyy-MM-dd"));
        
        try
        {
            await PerformExtractionAsync(ct);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
```

## Current Implementation Notes

### ScheduleCalculator Class
- **Purpose**: Centralized scheduling logic that was previously embedded in `ScheduledExtractor`
- **Dependencies**: `ApplicationConfiguration` and `TimeProvider` for time calculations
- **Benefits**: Better separation of concerns, easier testing, focused responsibility
- **Interface**: `IScheduleCalculator` defines the contract for scheduling calculations

### Enhanced ApplicationConfiguration
- **Validation**: All numeric properties validate values at the setter level
- **Properties**: `ExtractIntervalMinutes`, `RetryAttempts`, and `RetryDelaySeconds` must be > 0
- **Benefits**: Early validation prevents invalid configuration from propagating through the system
- **Error Handling**: Clear error messages with parameter names for better debugging

### ScheduledExtractor
- **Dependencies**: Depends on `IScheduleCalculator` for scheduling calculations
- **Responsibility**: Focused on background service orchestration rather than scheduling calculations
- **Benefits**: Cleaner separation of concerns, easier testing, more focused class

### Configuration Service
- **Custom Implementation**: Uses a custom `CommandLineParser` class instead of standard Microsoft configuration patterns
- **Command Line Parsing**: Manual parsing of command line arguments with custom logic
- **Configuration Merging**: Manual merging of configuration sources with custom precedence logic
- **Help Command**: The `--help` flag displays help and exits the application

### .NET Version
- **Target Framework**: .NET 9.0 (not .NET Core 8+ as mentioned in some documentation)
- **Package Versions**: All Microsoft.Extensions packages are version 9.0.8

### Dependencies
- **PowerService.dll**: Referenced directly in the DataAccess project
- **No External Configuration Libraries**: Does not use `Microsoft.Extensions.Configuration.CommandLine` package

## Conclusion

The current architecture provides a solid foundation that meets all specified requirements while maintaining clean separation of concerns and testability. The modular design allows for easy enhancement and adaptation to different deployment scenarios.

**Key strengths**:
- Clear separation of concerns
- Comprehensive error handling
- Configurable behavior
- Production-ready logging
- Timezone-aware processing
- Centralized scheduling logic with better testability
- Configuration validation at the source
- Custom configuration service for full control

**Areas for potential enhancement**:
- Parallel processing for performance
- Distributed coordination for scaling
- Enhanced security features
- Advanced monitoring and observability
- Database persistence for analytics
- Standard Microsoft configuration patterns for better integration

The architecture decisions prioritize maintainability, testability, and compliance with requirements while providing clear paths for future enhancements. The design includes dedicated scheduling logic and configuration validation, making the system robust and easy to test.

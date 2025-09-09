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

**Benefits**: Centralized service management, loose coupling, testability

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

**Benefits**: Clear contracts, loose coupling, testability

### 5. Configuration Management

**Decision**: Multi-source configuration with precedence hierarchy using CommandLineParser NuGet package.

**Implementation**: Command line arguments (highest) → Configuration file → Default values
- Uses CommandLineParser (v2.9.1) for robust argument parsing
- Property-level validation for `ExtractIntervalMinutes` (must be > 0)
- Automatic merging with command line precedence

**Benefits**: Flexible deployment, environment-specific configuration, early validation

### 6. Error Handling and Retry Logic

**Decision**: Implement retry mechanism with exponential backoff.

**Implementation**: 
- Built-in retry mechanism in PositionExtractor
- Comprehensive logging at all levels
- Error handling throughout the application

**Benefits**: Resilience against failures, production-ready error handling

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

**Benefits**: Better separation of concerns, focused classes, centralized dependencies

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

### 6. Validation Pattern
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

**Key Features**: CommandLineParser package, automatic merging, property validation

## Security Considerations

### Current Implementation
- No authentication/authorization (not required by specification)
- File system access limited to configured output directory
- No sensitive data exposure in logs

### Potential Enhancements
- File access permissions validation
- Output directory path validation
- Audit logging for file operations

## Performance & Scalability

### Current Characteristics
- **Performance**: Minimal latency (in-memory processing), limited by external service response
- **Scalability**: Horizontal (multiple instances), vertical (single-threaded limitation)
- **Bottlenecks**: External service calls and file I/O

### Alternative Approaches
- **Microservices**: Independent scaling with increased complexity
- **Event-Driven**: Real-time processing with eventual consistency challenges
- **CQRS**: Optimized read performance with data consistency complexity

## Enhancement Scenarios

### Performance Optimizations
- **Parallel Processing**: Concurrent trade processing for high-frequency requirements
- **Streaming**: Real-time processing with backpressure for large datasets

### Security Enhancements
- **Multi-Tenant**: Tenant isolation and access control
- **Data Encryption**: Encrypted file storage for sensitive data

### Scalability Enhancements
- **Distributed Processing**: Horizontal scaling with coordination locks
- **Database Persistence**: Hybrid storage for analytics and audit trails

### Monitoring & Observability
- **Metrics Collection**: Performance timing, trade counts, error tracking
- **Distributed Tracing**: End-to-end request tracing with activity correlation

## Technical Specifications

- **Target Framework**: .NET 9.0
- **Package Versions**: Microsoft.Extensions packages v9.0.8, CommandLineParser v2.9.1
- **Dependencies**: PowerService.dll (referenced in DataAccess project)

## Conclusion

The current architecture provides a solid foundation that meets all specified requirements while maintaining clean separation of concerns and testability. The modular design allows for easy enhancement and adaptation to different deployment scenarios.

**Key strengths**: Clean architecture, comprehensive error handling, timezone-aware processing, robust configuration management

**Enhancement opportunities**: Performance optimization, distributed scaling, security features, monitoring capabilities

The architecture decisions prioritize maintainability, testability, and compliance with requirements while providing clear paths for future enhancements.

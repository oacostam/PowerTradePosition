using System.Diagnostics.CodeAnalysis;

namespace PowerTradePosition.Domain.Domain;
[ExcludeFromCodeCoverage]
public record PowerTrade(DateTime Date, PowerPeriod[] Periods);

[ExcludeFromCodeCoverage]
public record PowerPeriod(int Period, double Volume);

[ExcludeFromCodeCoverage]
public record PowerPosition(DateTime DateTime, double Volume);
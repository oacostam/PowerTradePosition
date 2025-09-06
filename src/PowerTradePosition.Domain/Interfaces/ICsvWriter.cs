using PowerTradePosition.Domain.Domain;

namespace PowerTradePosition.Domain.Interfaces;

public interface ICsvWriter
{
    Task WriteToFileAsync(IEnumerable<PowerPosition> positions, DateTime dayAheadDate, DateTime extractionTime, string outputFolderPath, CancellationToken ct);
}
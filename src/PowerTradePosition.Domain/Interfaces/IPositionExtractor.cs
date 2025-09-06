namespace PowerTradePosition.Domain.Interfaces;

public interface IPositionExtractor
{
    Task ExtractPositionsAsync(CancellationToken ct);
}
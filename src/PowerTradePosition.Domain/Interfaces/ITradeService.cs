using PowerTradePosition.Domain.Domain;

namespace PowerTradePosition.Domain.Interfaces;

public interface ITradeService
{
    Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime dayAheadLocalDate, CancellationToken ct);
}
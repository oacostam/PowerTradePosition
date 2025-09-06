using PowerTradePosition.Domain.Domain;

namespace PowerTradePosition.Domain.Interfaces;

public interface IPositionAggregator
{
    IEnumerable<PowerPosition> AggregatePositionsByHour(IEnumerable<PowerTrade> trades, string timeZoneId);
}
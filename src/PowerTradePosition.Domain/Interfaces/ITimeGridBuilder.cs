namespace PowerTradePosition.Domain.Interfaces;

public interface ITimeGridBuilder
{
    IEnumerable<DateTime> BuildHourlyTimeGrid(DateTime date, string timeZoneId);
}
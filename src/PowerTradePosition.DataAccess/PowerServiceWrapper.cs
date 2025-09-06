using System.Diagnostics.CodeAnalysis;
using Axpo;
using Microsoft.Extensions.Logging;
using PowerTradePosition.Domain.Interfaces;
using PowerPeriod = PowerTradePosition.Domain.Domain.PowerPeriod;
using PowerTrade = PowerTradePosition.Domain.Domain.PowerTrade;

namespace PowerTradePosition.DataAccess;
[ExcludeFromCodeCoverage]
public class PowerServiceWrapper : ITradeService
{
    private readonly ILogger<PowerServiceWrapper> _logger;
    private readonly IPowerService _powerService;

    public PowerServiceWrapper(IPowerService powerService, ILogger<PowerServiceWrapper> logger)
    {
        _powerService = powerService;
        _logger = logger;
    }

    public async Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime dayAheadLocalDate, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Retrieving trades for date: {Date}", dayAheadLocalDate.ToString("yyyy-MM-dd"));
            var trades = await _powerService.GetTradesAsync(dayAheadLocalDate);
            return ConvertToPowerTrades(trades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trades for date: {Date}", dayAheadLocalDate.ToString("yyyy-MM-dd"));
            throw;
        }
    }

    // Adapter: map Axpo.PowerTrade objects into our domain model. We intentionally do not expose Axpo types beyond this boundary.
    private static IEnumerable<PowerTrade> ConvertToPowerTrades(IEnumerable<Axpo.PowerTrade>? trades)
    {
        return trades == null
            ? []
            : trades.Select(trade => new PowerTrade(trade.Date, ConvertToPowerPeriods(trade.Periods))).ToList();
    }

    private static PowerPeriod[] ConvertToPowerPeriods(IEnumerable<Axpo.PowerPeriod>? periods)
    {
        return periods == null
            ? []
            : periods.Select(period => new PowerPeriod(period.Period, period.Volume)).ToArray();
    }
}
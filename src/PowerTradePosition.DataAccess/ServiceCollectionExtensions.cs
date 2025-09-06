using System.Diagnostics.CodeAnalysis;
using Axpo;
using Microsoft.Extensions.DependencyInjection;
using PowerTradePosition.Domain.Domain;
using PowerTradePosition.Domain.Interfaces;

namespace PowerTradePosition.DataAccess;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    //Keeps the dependency injection setup for PowerTrade services in one place, to avoid referencing it in multiple projects.
    public static IServiceCollection AddPowerTradeServices(this IServiceCollection services)
    {
        services.AddScoped<IPowerService, PowerService>();
        services.AddScoped<ITradeService, PowerServiceWrapper>();
        services.AddScoped<IScheduleCalculator, ScheduleCalculator>();
        return services;
    }
}
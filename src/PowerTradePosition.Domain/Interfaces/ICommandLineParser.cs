using PowerTradePosition.Domain.Domain;

namespace PowerTradePosition.Domain.Interfaces;

public interface ICommandLineParser
{
    ApplicationConfiguration ParseConfiguration(string[] args);
}

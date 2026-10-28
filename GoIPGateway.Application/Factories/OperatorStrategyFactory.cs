using GoIPGateway.Domain.Enums;

namespace GoIPGateway.Application.Factories;

public interface IOperatorStrategyFactory
{
    IOperatorStrategy GetStrategy(OperatorType op);
}

public class OperatorStrategyFactory : IOperatorStrategyFactory
{
    private readonly IEnumerable<IOperatorStrategy> _strategies;

    public OperatorStrategyFactory(IEnumerable<IOperatorStrategy> strategies)
    {
        _strategies = strategies;
    }

    public IOperatorStrategy GetStrategy(OperatorType op)
    {
        var strategy = _strategies.FirstOrDefault(s => s.Operator == op);
        return strategy ?? _strategies.First(s => s.Operator == OperatorType.Vodafone);
    }
}

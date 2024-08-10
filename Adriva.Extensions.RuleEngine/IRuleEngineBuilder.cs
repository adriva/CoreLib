using System;

namespace Adriva.Extensions.RuleEngine
{
    public interface IRuleEngineBuilder
    {
        IRuleEngineBuilder UseRepository<TRepository, TRepositoryOptions>(Action<TRepositoryOptions> configure)
                    where TRepository : class, IRuleRepository
                    where TRepositoryOptions : class;
    }
}

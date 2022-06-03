using System;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.RuleEngine
{

    public static class RuleEngineExtenions
    {
        private class RuleEngineBuilder : IRuleEngineBuilder
        {
            private readonly IServiceCollection Services;

            public RuleEngineBuilder(IServiceCollection services)
            {
                this.Services = services;
            }

            public IRuleEngineBuilder UseRepository<TRepository, TRepositoryOptions>(Action<TRepositoryOptions> configure)
                    where TRepository : class, IRuleRepository
                    where TRepositoryOptions : class
            {
                this.Services.AddTransient<IRuleRepository, TRepository>();
                this.Services.Configure(configure);
                return this;
            }
        }

        public static IRuleEngineBuilder AddRuleEngine(this IServiceCollection services, Action<RuleEngineOptions> configure)
        {
            return services.AddRuleEngine<DefaultRuleEngine>(configure);
        }

        public static IRuleEngineBuilder AddRuleEngine<TRuleEngine>(this IServiceCollection services, Action<RuleEngineOptions> configure) where TRuleEngine : class, IRuleEngine
        {
            services.AddTransient<IRuleEngine, TRuleEngine>();
            services.Configure<RuleEngineOptions>(configure);
            return new RuleEngineBuilder(services);
        }
    }
}

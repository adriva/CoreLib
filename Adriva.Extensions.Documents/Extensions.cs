using Microsoft.Extensions.DependencyInjection;

namespace Adriva.Extensions.Documents
{
    public static class Extensions
    {
        public static IServiceCollection AddDocuments(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IDocumentManager, DocumentManager>();
            return services;
        }
    }
}

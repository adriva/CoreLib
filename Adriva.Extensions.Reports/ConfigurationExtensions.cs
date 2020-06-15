using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace Adriva.Extensions.Reports
{
    internal static class ConfigurationExtensions
    {
        private const string DataSourcesSectionName = "dataSources";
        private const string ParametersSectionName = "parameters";
        private const string FiltersSectionName = "filters";
        private const string OutputSectionName = "output";
        private const string ColumnsSectionName = "columnDefinitions";
        private const string RendererOptionsSectionName = "rendererOptions";

        public static IConfigurationSection GetDataSources(this IConfigurationRoot configuration)
        {
            return configuration.GetSection(ConfigurationExtensions.DataSourcesSectionName);
        }

        public static IDictionary<string, IConfigurationSection> GetDataSourceParameters(this IConfigurationRoot configuration)
        {
            var dataSourcesSection = configuration.GetDataSources();
            return dataSourcesSection.GetChildren().ToDictionary(x => x.Key, x => x.GetSection(ConfigurationExtensions.ParametersSectionName));
        }

        public static IDictionary<string, IConfigurationSection> GetFilterRendererOptions(this IConfigurationRoot configuration, int loop)
        {
            var rendererOptionsSection = configuration.GetSection($"{ConfigurationExtensions.FiltersSectionName}:{loop}:{ConfigurationExtensions.RendererOptionsSectionName}");
            if (!rendererOptionsSection.GetChildren().Any()) return new Dictionary<string, IConfigurationSection>();
            return rendererOptionsSection.GetChildren().ToDictionary(x => x.Key, x => x);
        }

        public static IDictionary<string, IConfigurationSection> GetOutputRendererOptions(this IConfigurationRoot configuration)
        {
            var rendererOptionsSection = configuration.GetSection($"{ConfigurationExtensions.OutputSectionName}:{ConfigurationExtensions.RendererOptionsSectionName}");
            if (!rendererOptionsSection.GetChildren().Any()) return new Dictionary<string, IConfigurationSection>();
            return rendererOptionsSection.GetChildren().ToDictionary(x => x.Key, x => x);
        }

        public static IDictionary<string, IConfigurationSection> GetColumnRendererOptions(this IConfigurationRoot configuration, int loop)
        {
            var rendererOptionsSection = configuration.GetSection($"{ConfigurationExtensions.OutputSectionName}:{ConfigurationExtensions.ColumnsSectionName}:{loop}:{ConfigurationExtensions.RendererOptionsSectionName}");
            if (!rendererOptionsSection.GetChildren().Any()) return new Dictionary<string, IConfigurationSection>();
            return rendererOptionsSection.GetChildren().ToDictionary(x => x.Key, x => x);
        }
    }
}
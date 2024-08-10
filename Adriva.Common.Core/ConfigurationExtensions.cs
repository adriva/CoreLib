using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Adriva.Common.Core
{
    public static class ConfigurationExtensions
    {

        public static IConfiguration LoadDefault(this IConfigurationBuilder builder, string identifier, bool isConfigFileRequired = false)
        {
            builder
                .AddJsonFile("appsettings.json", !isConfigFileRequired)
                .AddJsonFile($"appsettings.{identifier}.json", true)
                .AddEnvironmentVariables()
                .SetBasePath(Directory.GetCurrentDirectory());
            IConfiguration configuration = builder.Build();
            configuration.LoadRuntimeSettings();
            configuration.LoadConnectionStrings();
            return configuration;
        }

        public static void LoadConnectionStrings(this IConfiguration configuration)
        {
            configuration.LoadConnectionStrings<ConnectionStrings>();
        }

        public static void LoadConnectionStrings<T>(this IConfiguration configuration) where T : ConnectionStrings
        {
            var connectionStringsSection = configuration.GetSection("ConnectionStrings");
            T connectionStrings = connectionStringsSection.Get<T>();
            ConnectionStrings.Default = connectionStrings;
        }


        public static void LoadRuntimeSettings(this IConfiguration configuration)
        {
            configuration.LoadRuntimeSettings<RuntimeSettings>();
        }

        public static void LoadRuntimeSettings<T>(this IConfiguration configuration) where T : RuntimeSettings
        {
            var runtimeSettingsSection = configuration.GetSection("RuntimeSettings");
            T runtimeSettings = runtimeSettingsSection.Get<T>();
            RuntimeSettings.Default = runtimeSettings;
        }
    }
}

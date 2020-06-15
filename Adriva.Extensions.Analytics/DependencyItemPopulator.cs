using Adriva.AppInsights.Serialization.Contracts;
using Adriva.Extensions.Analytics.Entities;

namespace Adriva.Extensions.Analytics
{
    internal sealed class DependencyItemPopulator : AnalyticsItemPopulator
    {
        public override string TargetKey => "RemoteDependency";

        public override bool TryPopulate(Envelope envelope, ref AnalyticsItem analyticsItem)
        {
            if (!(envelope.EventData is RemoteDependencyData dependencyData)) return false;

            DependencyItem dependencyItem = new DependencyItem();

            dependencyItem.Name = dependencyData.Name;
            dependencyItem.Duration = dependencyData.DurationInMilliseconds;
            dependencyItem.IsSuccess = dependencyData.IsSuccess;
            dependencyItem.Type = dependencyData.Type;
            dependencyItem.Target = dependencyData.Target;

            if (dependencyData.Properties.TryGetBoolean("DeveloperMode", out bool isDeveloperMode)) dependencyItem.IsDeveloperMode = isDeveloperMode;
            if (dependencyData.Properties.TryGetString("AspNetCoreEnvironment", out string environment)) dependencyItem.Environment = environment;
            if (dependencyData.Properties.TryGetString("Input", out string input)) dependencyItem.Input = input;
            if (dependencyData.Properties.TryGetString("Output", out string output)) dependencyItem.Output = output;

            analyticsItem.Dependencies.Add(dependencyItem);


            return true;
        }
    }
}
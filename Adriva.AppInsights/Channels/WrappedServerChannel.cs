using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;

namespace Adriva.AppInsights.Channels
{
    public class WrappedServerChannel : ITelemetryChannel, ITelemetryModule
    {
        private readonly ServerTelemetryChannel ServerChannel;
        public bool? DeveloperMode
        {
            get => this.ServerChannel.DeveloperMode;
            set => this.ServerChannel.DeveloperMode = value;
        }
        public string EndpointAddress
        {
            get => this.ServerChannel.EndpointAddress;
            set => this.ServerChannel.EndpointAddress = value;
        }

        public WrappedServerChannel()
        {
            this.ServerChannel = new ServerTelemetryChannel();
        }
        public void Dispose()
        {
            this.ServerChannel.Dispose();
        }

        public void Flush()
        {
            this.ServerChannel.Flush();
        }

        public void Send(ITelemetry item)
        {
            this.ServerChannel.Send(item);
        }

        public void Initialize(TelemetryConfiguration configuration)
        {
            this.ServerChannel.Initialize(configuration);
        }
    }
}
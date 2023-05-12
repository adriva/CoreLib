using Adriva.Common.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.Host.Queues;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Adriva.Worker.Core
{
    public abstract class WorkerHost
    {
        private IHost Host;

        protected IConfiguration Configuration { get; private set; }

        private int StartCalled = 0;

        protected IServiceProvider ServiceProvider => this.Host?.Services;

        protected virtual void ConfigureQueueOptions(QueueOptions options)
        {

        }

        protected virtual void ConfigureServices(IServiceCollection services)
        {

        }

        protected virtual void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
        {
            builder.SetMinimumLevel(LogLevel.Trace);
            builder.AddConsole();
        }

        /*protected virtual IConfiguration ConfigureAppConfiguration(IConfigurationBuilder builder, string environmentName)
        {
            return builder.LoadDefault(environmentName);
        }*/

        protected virtual void ConfigureWebJobs(IWebJobsBuilder builder)
        {

        }

        protected virtual void ConfigureHostBuilder(IHostBuilder hostBuilder)
        {

        }

        protected virtual IConfiguration ConfigureConfiguration(IConfigurationBuilder builder, string environmentName)
        {
            return builder.LoadDefault(environmentName);
        }

        protected virtual string ResolveEnvironmentName()
        {
            string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            return string.IsNullOrWhiteSpace(environmentName) ? "Development" : environmentName;
        }

        protected virtual Type ResolveTypeLocator() => null;

        protected virtual Type ResolveJobActivator() => null;

        protected virtual void Initialize(WorkerHostOptions hostOptions = null)
        {
            hostOptions = hostOptions ?? new WorkerHostOptions();

            string environmentName = this.ResolveEnvironmentName();
            var hostBuilder = new HostBuilder()
                .UseEnvironment(environmentName)
                .ConfigureHostConfiguration((builder) =>
                {
                    this.Configuration = this.ConfigureConfiguration(builder, environmentName);
                })
                //.ConfigureAppConfiguration((builder) =>
                //{
                //    this.Configuration = this.ConfigureAppConfiguration(builder, environmentName);
                //})
                .ConfigureLogging(this.ConfigureLogging)
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging();
                    services.AddWebJobs(options => { });
                    this.ConfigureServices(services);
                    services.Configure<QueueOptions>(this.ConfigureQueueOptions);

                    IWebJobsBuilder webJobsBuilder = services.AddWebJobs(options => { });

                    if (hostOptions.UseBuiltInBindings) webJobsBuilder.AddBuiltInBindings();
                    if (hostOptions.UseAzureStorageCoreServices | hostOptions.UseAzureStorage) webJobsBuilder.AddAzureStorageCoreServices();
                    if (hostOptions.UseAzureStorage) webJobsBuilder.AddAzureStorage();
                    if (hostOptions.UseTimers) webJobsBuilder.AddTimers();

                    this.ConfigureWebJobs(webJobsBuilder);

                    Type jobActivatorType = this.ResolveJobActivator();

                    webJobsBuilder.Services.AddSingleton(typeof(IJobActivator), jobActivatorType ?? typeof(JobActivatorBase));
                    webJobsBuilder.Services.TryAddSingleton<IQueueProcessorFactory, QueueProcessorFactoryBase>();

                    Type typeLocatorType = this.ResolveTypeLocator();

                    if (null != typeLocatorType)
                    {
                        webJobsBuilder.Services.AddSingleton(typeof(ITypeLocator), typeLocatorType);
                    }

                    services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, JobHostService>());
                })
                .UseConsoleLifetime();
            this.ConfigureHostBuilder(hostBuilder);
            this.Host = hostBuilder.Build();
        }

        public async Task StartAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (1 == Interlocked.CompareExchange(ref this.StartCalled, 1, 0))
            {
                return;
            }

            this.Initialize();
            await this.Host.RunAsync(cancellationToken);
        }

        public async Task StopAsync(TimeSpan timeout)
        {
            await this.Host?.StopAsync(timeout);
        }
    }
}

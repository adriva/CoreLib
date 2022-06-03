using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Adriva.Extensions.Http;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Adriva.AppInsights.Channels
{

    internal sealed class PersistentChannel : ITelemetryChannel, IDisposable
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly TelemetryBuffer Buffer;
        private readonly SemaphoreSlim TransmitSemaphore;
        private readonly HttpClientWrapper HttpClient;
        private readonly AnalyticsOptions Options;
        private readonly object FileLock = new object();
        private readonly CancellationTokenSource ApplicationStopTokenSource = new CancellationTokenSource();

        private long BacklogSize = 0;
        private bool IsWorkingFolderReady;
        private Uri EndpointUri;
        private int IsProcessingBackLog;

        public bool? DeveloperMode { get; set; }

        public string EndpointAddress
        {
            get => this.EndpointUri?.ToString();
            set
            {
                if (!Uri.TryCreate(value, UriKind.Absolute, out Uri uri))
                {
                    this.EndpointUri = null;
                    throw new UriFormatException("Invalid endpoint Uri specified.");
                }

                this.EndpointUri = uri;
            }
        }

        public string LocalFolder { get; set; }

        public PersistentChannel(IServiceProvider serviceProvider, IOptions<AnalyticsOptions> optionsAccessor)
        {
            this.Options = optionsAccessor.Value;

            this.TransmitSemaphore = new SemaphoreSlim(1, 1);

            this.HttpClient = new HttpClientWrapper(new HttpClient(), new CookieContainer());
            this.LocalFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            this.Buffer = new TelemetryBuffer();
            this.Buffer.OnFull = this.OnBufferFull;
            this.Buffer.BacklogSize = this.Options.BacklogSize;
            this.Buffer.Capacity = this.Options.Capacity;

            this.ServiceProvider = serviceProvider;
        }

        private string GetWorkingFolder()
        {
            string workingFolder = Path.Combine(this.LocalFolder, "analytics");

            if (!this.IsWorkingFolderReady)
            {
                lock (this.FileLock)
                {
                    if (!this.IsWorkingFolderReady)
                    {
                        if (!Directory.Exists(workingFolder))
                        {
                            Directory.CreateDirectory(workingFolder);
                        }
                        else
                        {

                            var filePaths = Directory.EnumerateFiles(workingFolder, "*.ai");
                            try
                            {
                                foreach (var filePath in filePaths)
                                {
                                    File.Move(filePath, $"{filePath}.fail");
                                }
                            }
                            catch { }


                        }
                        this.IsWorkingFolderReady = true;
                    }

                    var applicationLifetime = this.ServiceProvider.GetService<IApplicationLifetime>();
                    applicationLifetime.ApplicationStopping.Register((state) =>
                    {
                        if (state is CancellationTokenSource cancellationTokenSource)
                        {
                            cancellationTokenSource.Cancel(false);
                        }
                    }, this.ApplicationStopTokenSource);
                }
            }

            return workingFolder;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Potential Code Quality Issues", "RECS0022:A catch clause that catches System.Exception and has an empty body", Justification = "<Pending>")]
        private void OnBufferFull()
        {
            var items = this.Buffer.Dequeue();

            _ = this.TransmitAsync(items);

            if (0 == Interlocked.CompareExchange(ref this.IsProcessingBackLog, 1, 0))
            {
                _ = this.TransmitFailedLogsAsync(this.ApplicationStopTokenSource.Token);
            }
        }

        private async Task TransmitAsync(IEnumerable<ITelemetry> items)
        {
            if (null == items || !items.Any())
            {
                return;
            }

            if (!await this.TransmitSemaphore.WaitAsync(1000)) return;

            byte[] buffer = JsonSerializer.Serialize(items, true);

            string filePath = Path.Combine(this.GetWorkingFolder(), $"{Guid.NewGuid().ToString("N")}.ai");
            try
            {
                using (var fileTransaction = FileTransaction.Create(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await fileTransaction.WriteAsync(buffer);
                    using (var content = new ByteArrayContent(buffer))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue(JsonSerializer.ContentType);
                        await this.HttpClient.PostAsync(this.EndpointUri.ToString(), content, null);
                    }
                    fileTransaction.Commit();
                }
            }
            catch
            {
                if (this.BacklogSize >= this.Options.BacklogSize)
                {
                    File.Delete(filePath);
                }
                else
                {
                    this.BacklogSize += items.LongCount();
                    File.Move(filePath, $"{filePath}.fail");
                }
            }
            finally
            {
                this.TransmitSemaphore.Release();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Potential Code Quality Issues", "RECS0022:A catch clause that catches System.Exception and has an empty body", Justification = "<Pending>")]
        private async Task TransmitFailedLogsAsync(CancellationToken cancellationToken)
        {
            string workingFolder = this.GetWorkingFolder();

            var filePaths = Directory.EnumerateFiles(workingFolder, "*.ai.fail");

            try
            {
                foreach (var filePath in filePaths)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    try
                    {
                        using (var transaction = FileTransaction.Create(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            byte[] buffer = File.ReadAllBytes(filePath);
                            using (var content = new ByteArrayContent(buffer))
                            {
                                content.Headers.ContentType = new MediaTypeHeaderValue(JsonSerializer.ContentType);
                                await this.HttpClient.PostAsync(this.EndpointUri.ToString(), content, null);
                            }
                            transaction.Commit();
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                Interlocked.Exchange(ref this.IsProcessingBackLog, 0);
            }
        }


        public void Send(ITelemetry item)
        {
            this.Buffer.Enqueue(item);
        }

        public void Flush()
        {
            this.OnBufferFull();
        }

        public void Dispose()
        {
            this.TransmitSemaphore?.Dispose();
        }
    }
}
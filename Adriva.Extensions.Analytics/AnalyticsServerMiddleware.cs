using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using AiJsonSerializer = Microsoft.ApplicationInsights.Extensibility.Implementation.JsonSerializer;
using System.Linq;
using Newtonsoft.Json;
using Adriva.AppInsights.Serialization;
using System.IO;
using System.IO.Compression;
using Adriva.AppInsights.Serialization.Contracts;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace Adriva.Extensions.Analytics
{
    public class AnalyticsServerMiddleware
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings;
        private readonly RequestDelegate Next;
        private readonly AnalyticsServerOptions Options;
        private readonly IQueueingService QueueingService;
        private readonly ILogger<AnalyticsServerMiddleware> Logger;

        static AnalyticsServerMiddleware()
        {
            AnalyticsServerMiddleware.JsonSerializerSettings = new JsonSerializerSettings();
            AnalyticsServerMiddleware.JsonSerializerSettings.Converters.Add(new TelemetryConverter());
        }

        public AnalyticsServerMiddleware(RequestDelegate next, IQueueingService queueingService, ILogger<AnalyticsServerMiddleware> logger, IOptions<AnalyticsServerOptions> optionsAccessor)
        {
            this.Options = optionsAccessor.Value;
            this.Next = next;
            this.QueueingService = queueingService;
            this.Logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var request = context.Request;

            if (HttpMethods.IsPost(request.Method))
            {

                this.Logger.LogInformation("Request received by Analytics middleware.");

                Stream inputStream = request.Body;

                if (request.Headers.TryGetValue(HeaderNames.ContentType, out StringValues contentTypeHeader))
                {
                    if (contentTypeHeader.Any(ct => 0 == string.Compare(ct, AiJsonSerializer.ContentType, StringComparison.OrdinalIgnoreCase)))
                    {
                        this.Logger.LogInformation("Analytics Middleware received compressed JSON data.");
                        inputStream = new GZipStream(request.Body, CompressionMode.Decompress);
                    }

                }
                this.Logger.LogInformation("Extracting envelope items from the request body.");
                var envelopeItems = NdJsonSerializer.Deserialize<Envelope>(inputStream, AnalyticsServerMiddleware.JsonSerializerSettings).ToList();
                this.Logger.LogInformation($"Extracted {envelopeItems.Count} envelope items.");

                if (0 < envelopeItems.Count)
                {
                    foreach (var envelopeItem in envelopeItems)
                    {
                        this.QueueingService.Queue(envelopeItem);
                    }
                }
                inputStream?.Dispose();
                context.Response.StatusCode = 204;
                return;
            }

            this.Logger.LogTrace("Passing the request to next handler.");
            await this.Next.Invoke(context);
        }
    }
}

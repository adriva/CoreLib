using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Adriva.AppInsights
{
    public class RemoteDependencyDataInitializer : ITelemetryInitializer
    {
        private static bool IsTextBasedContentType(HttpHeaders headers)
        {
            if (!headers.TryGetValues("Content-Type", out var values))
                return false;

            var header = string.Join(" ", values).ToLowerInvariant();
            var textBasedTypes = new[] { "html", "text", "xml", "json", "txt", "x-www-form-urlencoded" };
            return textBasedTypes.Any(t => header.Contains(t));
        }

        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is DependencyTelemetry dependencyTelemetry)
            {
                if (0 == string.Compare("Http", dependencyTelemetry.Type))
                {
                    if (dependencyTelemetry.TryGetOperationDetail("HttpRequest", out object request) && request is HttpRequestMessage httpRequestMessage)
                    {
                        if (dependencyTelemetry.TryGetOperationDetail("HttpResponse", out object response) && response is HttpResponseMessage httpResponseMessage)
                        {
                            if ((HttpMethod.Get == httpRequestMessage.Method || RemoteDependencyDataInitializer.IsTextBasedContentType(httpRequestMessage.Headers)) && RemoteDependencyDataInitializer.IsTextBasedContentType(httpResponseMessage.Content.Headers))
                            {
                                string requestContent = null;
                                string responseContent = null;

                                if (null != httpRequestMessage.Content)
                                {
                                    requestContent = httpRequestMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                }
                                if (null != httpResponseMessage.Content)
                                {
                                    responseContent = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                                }

                                dependencyTelemetry.Properties.Add("Input", requestContent);
                                dependencyTelemetry.Properties.Add("Output", responseContent);
                            }
                        }
                    }
                }
            }
        }
    }
}
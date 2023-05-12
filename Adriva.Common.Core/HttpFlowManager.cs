using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Adriva.Common.Core
{
	public sealed class HttpFlowManager : IDisposable
	{
		private CookieContainer CookieContainer;
		private HttpClientHandler Handler;
		private HttpClient HttpClient;
		private bool IsInitialized = false;

		private readonly LinkedList<Func<HttpResponseMessage, Task<HttpResponseMessage>>> Steps = new LinkedList<Func<HttpResponseMessage, Task<HttpResponseMessage>>>();

		public void AddStep(HttpMethod method, string url, Action<HttpResponseMessage, HttpRequestMessage> requestHandler)
		{
			async Task<HttpResponseMessage> stepAction(HttpResponseMessage previousResponse)
			{
				HttpRequestMessage request = new HttpRequestMessage(method, url);
				requestHandler?.Invoke(previousResponse, request);
				return await this.HttpClient.SendAsync(request);
			}

			this.Steps.AddLast(stepAction);
		}

		public async Task<HttpResponseMessage> RunAsync()
		{
			if (0 == this.Steps.Count) return null;

			if (!this.IsInitialized)
			{
				this.CookieContainer = new CookieContainer();
				this.Handler = new HttpClientHandler() { CookieContainer = this.CookieContainer, AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip };
				this.HttpClient = new HttpClient(this.Handler);
				this.IsInitialized = true;
			}

			var currentStepNode = this.Steps.First;
			HttpResponseMessage lastResponse = null;

			while (null != currentStepNode)
			{
				var newResponse = await currentStepNode.Value.Invoke(lastResponse);
				currentStepNode = currentStepNode.Next;
				lastResponse = newResponse;
			}

			return lastResponse;
		}

		public async Task<HttpResponseMessage> ExecuteAsync(HttpRequestMessage request)
		{
			return await this.HttpClient.SendAsync(request);
		}

		public void Dispose()
		{
			if (null != this.Handler)
			{
				this.Handler.Dispose();
				this.Handler = null;
			}

			if (null != this.HttpClient)
			{
				this.HttpClient.Dispose();
				this.HttpClient = null;
			}
		}
	}
}

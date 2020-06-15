using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;

namespace Adriva.Extensions.Http
{
    public class HttpClientWrapper
    {
        private readonly HttpClient HttpClient;
        private readonly CookieContainer CookieContainer;

        public HttpClientWrapper(HttpClient httpClient, CookieContainer cookieContainer)
        {
            this.HttpClient = httpClient;
            this.CookieContainer = cookieContainer;
        }

        protected virtual void PopulateHttpHeaders(Dictionary<string, string> headers)
        {
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.96 Safari/537.36");
        }

        public void AddCookie(Uri uri, Cookie cookie)
        {
            this.CookieContainer.Add(uri, cookie);
        }

        public void SetCookies(Uri uri, string cookieHeader)
        {
            this.CookieContainer.SetCookies(uri, cookieHeader);
        }

        public CookieCollection GetCookies(Uri uri)
        {
            return this.CookieContainer.GetCookies(uri);
        }

        #region Http Head Overloads

        public async Task<HttpResponseMessage> HeadAsync(string url, bool shouldCheckStatusCode = false)
        {
            var headers = new Dictionary<string, string>();
            this.PopulateHttpHeaders(headers);
            return await this.HeadAsync(url, headers, shouldCheckStatusCode);
        }

        public async Task<HttpResponseMessage> HeadAsync(string url, Dictionary<string, string> headers, bool shouldCheckStatusCode)
        {
            if (null != headers)
            {
                this.HttpClient.DefaultRequestHeaders.Clear();
                foreach (var header in headers)
                {
                    this.HttpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            using (var request = new HttpRequestMessage(HttpMethod.Head, url))
            {
                var response = await this.HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                if (shouldCheckStatusCode) response.EnsureSuccessStatusCode();
                return response;
            }
        }


        #endregion

        #region Http Get Overloads

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await this.GetAsync(url, true);
        }

        public async Task<HttpResponseMessage> GetAsync(string url, bool shouldCheckStatusCode)
        {
            var headers = new Dictionary<string, string>();
            this.PopulateHttpHeaders(headers);
            return await this.GetAsync(url, headers, shouldCheckStatusCode);
        }

        public async Task<HttpResponseMessage> GetAsync(string url, bool shouldCheckStatusCode, HttpCompletionOption httpCompletionOption)
        {
            var headers = new Dictionary<string, string>();
            this.PopulateHttpHeaders(headers);
            return await this.GetAsync(url, headers, shouldCheckStatusCode, httpCompletionOption);
        }

        public async Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers, bool shouldCheckStatusCode)
        {
            return await this.GetAsync(url, headers, shouldCheckStatusCode, HttpCompletionOption.ResponseContentRead);
        }

        public async Task<HttpResponseMessage> GetAsync(string url, Dictionary<string, string> headers, bool shouldCheckStatusCode, HttpCompletionOption httpCompletionOption)
        {
            if (null != headers)
            {
                this.HttpClient.DefaultRequestHeaders.Clear();
                foreach (var header in headers)
                {
                    this.HttpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var response = await this.HttpClient.GetAsync(url, httpCompletionOption);
            if (shouldCheckStatusCode) response.EnsureSuccessStatusCode();
            return response;
        }

        #endregion

        #region Http Post Overloads

        public async Task<HttpResponseMessage> PostAsync<T>(string url, T data) where T : HttpContent
        {
            var headers = new Dictionary<string, string>();
            this.PopulateHttpHeaders(headers);
            return await this.PostAsync<T>(url, data, headers);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent content, Dictionary<string, string> headers)
        {

            if (null != headers)
            {
                foreach (var header in headers)
                {
                    this.HttpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var response = await this.HttpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();
            return response;

        }

        public async Task<HttpResponseMessage> PostAsync<T>(string url, T data, Dictionary<string, string> headers) where T : HttpContent
        {
            if (null != headers)
            {
                foreach (var header in headers)
                {
                    this.HttpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            var response = await this.HttpClient.PostAsync(url, data);
            response.EnsureSuccessStatusCode();
            return response;
        }

        #endregion

    }

}
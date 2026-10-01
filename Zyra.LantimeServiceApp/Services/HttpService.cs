using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly ZyraIntegrationCredentials _credentials;
        private readonly ISessionService _sessionService;

        public HttpService(
            IOptions<ZyraIntegrationCredentials> options,
            HttpClient httpClient,
            ISessionService sessionService)
        {
            _httpClient = httpClient;
            _credentials = options.Value;
            _sessionService = sessionService;
        }

        public async Task PostAsync<T>(string url, T data)
        {
            var result = await PostWithResultAsync(url, data);

            if (!result.IsSuccess)
            {
                throw new Exception(
                    $"API Error: {(HttpStatusCode)result.StatusCode} - {result.ResponseBody}");
            }
        }

        public async Task<HttpApiResult> PostWithResultAsync<T>(string url, T data)
        {
            var token = await _sessionService.GetTokenAsync();

            var response = await SendPostAsync(url, data, token);

            // Handle expired token.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                token = await _sessionService.GetTokenAsync();
                response = await SendPostAsync(url, data, token);
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            return new HttpApiResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                ReasonPhrase = response.ReasonPhrase ?? string.Empty,
                ResponseBody = responseBody
            };
        }

        private async Task<HttpResponseMessage> SendPostAsync<T>(
            string url,
            T data,
            string token)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Content = JsonContent.Create(data);

            return await _httpClient.SendAsync(request);
        }
    }
}

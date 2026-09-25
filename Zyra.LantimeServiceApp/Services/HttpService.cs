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

        public HttpService(IOptions<ZyraIntegrationCredentials> options, HttpClient httpClient, ISessionService sessionService)
        {
            _httpClient = httpClient;
            _credentials = options.Value;
            _sessionService = sessionService;
        }

        public async Task PostAsync<T>(string url, T data)
        {
            // Get token (will auto fetch from cache or regenerate)
            var token = await _sessionService.GetTokenAsync();

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(data);

            var response = await _httpClient.SendAsync(request);

            // Handle expired token (very important)
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Force refresh (clear cache inside session service)
                token = await _sessionService.GetTokenAsync();

                request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
                request.Content = JsonContent.Create(data);

                response = await _httpClient.SendAsync(request);
            }

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"API Error: {response.StatusCode} - {error}");
            }
        }


    }
}

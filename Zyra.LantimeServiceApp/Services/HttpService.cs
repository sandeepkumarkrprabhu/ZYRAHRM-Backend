using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly ZyraIntegrationCredentials _credentials;
        private readonly ISessionService _sessionService;
        private readonly ILogger<HttpService> _logger;

        public HttpService(
            IOptions<ZyraIntegrationCredentials> options,
            HttpClient httpClient,
            ISessionService sessionService,
            ILogger<HttpService> logger)
        {
            _httpClient = httpClient;
            _credentials = options.Value;
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task PostAsync<T>(string url, T data)
        {
            var requestBody = JsonSerializer.Serialize(data);
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ZYRAHRM API REQUEST | Method=POST | Url={Url} | Request={Request}",
                url,
                requestBody);

            try
            {
                var token = await _sessionService.GetTokenAsync();

                var response = await SendPostAsync(url, data, token);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning(
                        "ZYRAHRM API RESPONSE | Method=POST | Url={Url} | StatusCode={StatusCode} | Response={Response} | Action=TokenRefresh",
                        url,
                        (int)response.StatusCode,
                        responseBody);

                    token = await _sessionService.GetTokenAsync();
                    response = await SendPostAsync(url, data, token);
                    responseBody = await response.Content.ReadAsStringAsync();
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ZYRAHRM API RESPONSE | Method=POST | Url={Url} | StatusCode={StatusCode} | Success={Success} | DurationMs={DurationMs} | Response={Response}",
                    url,
                    (int)response.StatusCode,
                    response.IsSuccessStatusCode,
                    stopwatch.ElapsedMilliseconds,
                    responseBody);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"API Error: {(int)response.StatusCode} - {responseBody}");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "ZYRAHRM API FAILURE | Method=POST | Url={Url} | DurationMs={DurationMs} | Request={Request} | Exception={ExceptionType} | Message={Message}",
                    url,
                    stopwatch.ElapsedMilliseconds,
                    requestBody,
                    ex.GetType().Name,
                    ex.Message);

                throw;
            }
        }

        private async Task<HttpResponseMessage> SendPostAsync<T>(
            string url,
            T data,
            string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            request.Content = JsonContent.Create(data);

            return await _httpClient.SendAsync(request);
        }
    }
}

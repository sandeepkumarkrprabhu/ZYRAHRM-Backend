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
            var employeeCode = GetPropertyValue(data, "employee_code", "EmployeeCode");
            var biometricUserId = GetPropertyValue(
                data,
                "biometric_user_id",
                "BiometricUserId",
                "biometricUserId");

            var logDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ZYRAHRM_API | DateTime={DateTime} | HRMEmployeeCode={HRMEmployeeCode} | BiometricUserId={BiometricUserId} | Method=POST | Url={Url} | Event=REQUEST | Request={Request}",
                logDateTime,
                employeeCode ?? "-",
                biometricUserId ?? "-",
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
                        "ZYRAHRM_API | DateTime={DateTime} | HRMEmployeeCode={HRMEmployeeCode} | BiometricUserId={BiometricUserId} | Method=POST | Url={Url} | Event=UNAUTHORIZED | StatusCode={StatusCode} | Response={Response} | Action=TokenRefresh",
                        DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        employeeCode ?? "-",
                        biometricUserId ?? "-",
                        url,
                        (int)response.StatusCode,
                        responseBody);

                    token = await _sessionService.GetTokenAsync();
                    response = await SendPostAsync(url, data, token);
                    responseBody = await response.Content.ReadAsStringAsync();
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "ZYRAHRM_API | DateTime={DateTime} | HRMEmployeeCode={HRMEmployeeCode} | BiometricUserId={BiometricUserId} | Method=POST | Url={Url} | Event=RESPONSE | StatusCode={StatusCode} | Success={Success} | DurationMs={DurationMs} | Response={Response}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    employeeCode ?? "-",
                    biometricUserId ?? "-",
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
                    "ZYRAHRM_API | DateTime={DateTime} | HRMEmployeeCode={HRMEmployeeCode} | BiometricUserId={BiometricUserId} | Method=POST | Url={Url} | Event=FAILURE | DurationMs={DurationMs} | Request={Request} | Exception={ExceptionType} | Message={Message}",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    employeeCode ?? "-",
                    biometricUserId ?? "-",
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

        private static string? GetPropertyValue<T>(T data, params string[] propertyNames)
        {
            if (data == null)
                return null;

            var type = data.GetType();

            foreach (var propertyName in propertyNames)
            {
                var property = type.GetProperty(propertyName);

                if (property?.GetValue(data) is object value)
                {
                    var stringValue = value.ToString()?.Trim();

                    if (!string.IsNullOrWhiteSpace(stringValue))
                        return stringValue;
                }
            }

            return null;
        }
    }
}

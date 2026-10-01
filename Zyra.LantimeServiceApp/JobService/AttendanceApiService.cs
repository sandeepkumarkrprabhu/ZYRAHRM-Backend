using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class AttendanceApiService : IAttendanceApiService
    {
        private readonly IHttpService _httpService;
        private readonly ILogger<AttendanceApiService> _logger;
        private readonly ZyraIntegrationCredentials _credentials;

        private readonly string _endpoint = "api/attendance/action";

        public AttendanceApiService(
            IHttpService httpService,
            IOptions<ZyraIntegrationCredentials> options,
            ILogger<AttendanceApiService> logger)
        {
            _httpService = httpService;
            _logger = logger;
            _credentials = options.Value;
        }

        public async Task<bool> SendAsync(AttendanceAPIDto request)
        {
            if (request == null)
            {
                _logger.LogWarning("Attendance request is null");
                return false;
            }

            var url = BuildUrl();

            try
            {
                _logger.LogInformation(
                    "Attendance API REQUEST | URL: {Url} | Employee: {Employee} | Type: {Type} | Time: {Time}",
                    url,
                    request.employee_code,
                    request.type,
                    request.date_time);

                var result = await _httpService.PostWithResultAsync(url, request);

                _logger.LogInformation(
                    "Attendance API RESPONSE | Employee: {Employee} | Type: {Type} | Time: {Time} | " +
                    "StatusCode: {StatusCode} | Reason: {Reason} | Success: {Success} | Response: {Response}",
                    request.employee_code,
                    request.type,
                    request.date_time,
                    result.StatusCode,
                    result.ReasonPhrase,
                    result.IsSuccess,
                    result.ResponseBody);

                if (!result.IsSuccess)
                {
                    _logger.LogError(
                        "Attendance API FAILED | Employee: {Employee} | Type: {Type} | Time: {Time} | " +
                        "StatusCode: {StatusCode} | Response: {Response}",
                        request.employee_code,
                        request.type,
                        request.date_time,
                        result.StatusCode,
                        result.ResponseBody);

                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance API EXCEPTION | Employee: {Employee} | Type: {Type} | Time: {Time} | URL: {Url}",
                    request.employee_code,
                    request.type,
                    request.date_time,
                    url);

                return false;
            }
        }

        private string BuildUrl()
        {
            var baseUrl = _credentials?.Domain?.TrimEnd('/');

            if (string.IsNullOrEmpty(baseUrl))
                throw new InvalidOperationException("Attendance API base URL is missing");

            return $"{baseUrl}/{_endpoint}";
        }
    }
}

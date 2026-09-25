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
                    "Sending attendance request: {Employee} - {Type} - {Time}",
                    request.employee_code,
                    request.type,
                    request.date_time);

                await _httpService.PostAsync(url, request);

                _logger.LogInformation(
                    "Attendance API success for {Employee}",
                    request.employee_code);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Attendance API failed for {Employee} | Type: {Type} | Time: {Time}",
                    request.employee_code,
                    request.type,
                    request.date_time);

                return false;
            }
        }

        // -------------------------------------------------
        // PRIVATE: URL BUILDER
        // -------------------------------------------------
        private string BuildUrl()
        {
            var baseUrl = _credentials?.Domain?.TrimEnd('/');

            if (string.IsNullOrEmpty(baseUrl))
                throw new InvalidOperationException("Attendance API base URL is missing");

            return $"{baseUrl}/{_endpoint}";
        }
    }
}

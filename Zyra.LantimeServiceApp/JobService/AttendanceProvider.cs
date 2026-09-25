using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class AttendanceProvider : IAttendanceProvider
    {
        private readonly IAttendanceDbService _dbService;
        private readonly ILogger<AttendanceProvider> _logger;

        public AttendanceProvider(
            IAttendanceDbService dbService,
            ILogger<AttendanceProvider> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public async Task<List<AttendanceDto>> GetAttendanceAsync(DateTime fromDate)
        {
            try
            {
                var data = await _dbService.GetAttendanceAsync(fromDate);

                if (data == null || data.Count == 0)
                {
                    _logger.LogWarning("No attendance records found for {Date}", fromDate);
                    return new List<AttendanceDto>();
                }

                var cleaned = data
                    .Where(x => !string.IsNullOrEmpty(x.EmployeeCode))
                    .Select(x =>
                    {
                        x.EmployeeName ??= "Unknown";
                        return x;
                    })
                    .ToList();

                _logger.LogInformation(
                    "AttendanceProvider returned {Count} records",
                    cleaned.Count);

                return cleaned;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AttendanceProvider.GetAttendanceAsync");
                throw;
            }
        }

        public async Task<List<BiometricPunch>> GetPunchesAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            try
            {
                var data = await _dbService.GetPunchesAsync(fromDate, toDate);

                _logger.LogInformation(
                    "AttendanceProvider returned {Count} biometric punches between {From} and {To}",
                    data.Count,
                    fromDate,
                    toDate);

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error in AttendanceProvider.GetPunchesAsync");
                throw;
            }
        }

        public async Task<List<EmployeeMapping>> GetLastPunchDataAsync()
        {
            try
            {
                var data = await _dbService.GetLastPunchTime();
                return data ?? new List<EmployeeMapping>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching last punch data");
                throw;
            }
        }

        public async Task<List<EmployeeMapping>> GetNewEmployeesAsync()
        {
            try
            {
                var data = await _dbService.GetNewEmployees();
                return data ?? new List<EmployeeMapping>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching new employees");
                throw;
            }
        }
    }
}
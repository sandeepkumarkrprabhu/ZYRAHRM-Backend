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

                return data
                    .Where(x => !string.IsNullOrWhiteSpace(x.EmployeeCode))
                    .Select(x =>
                    {
                        x.EmployeeName ??= "Unknown";
                        return x;
                    })
                    .ToList();
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
                return await _dbService.GetPunchesAsync(fromDate, toDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error fetching biometric punches between {FromDate} and {ToDate}",
                    fromDate,
                    toDate);
                throw;
            }
        }

        public async Task<List<EmployeeMapping>> GetLastPunchDataAsync()
        {
            try
            {
                return await _dbService.GetLastPunchTime()
                    ?? new List<EmployeeMapping>();
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
                return await _dbService.GetNewEmployees()
                    ?? new List<EmployeeMapping>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching new employees");
                throw;
            }
        }
    }
}
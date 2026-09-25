using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        // -------------------------------
        // MAIN METHOD (USED BY JOBS)
        // -------------------------------
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

                // -------------------------------
                // NORMALIZATION LAYER
                // -------------------------------
                var cleaned = data
                    .Where(x => !string.IsNullOrEmpty(x.EmployeeCode))
                    .Select(x =>
                    {
                        // Safety normalization
                        x.EmployeeName ??= "Unknown";

                        return x;
                    })
                    .ToList();

                _logger.LogInformation("AttendanceProvider returned {Count} records", cleaned.Count);

                return cleaned;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AttendanceProvider.GetAttendanceAsync");
                throw;
            }
        }

        // -------------------------------
        // OPTIONAL: LAST PUNCH DATA
        // -------------------------------
        public async Task<List<EmployeeMapping>> GetLastPunchDataAsync()
        {
            try
            {
                var data = await _dbService.GetLastPunchTime();

                if (data == null)
                    return new List<EmployeeMapping>();

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching last punch data");
                throw;
            }
        }

        // -------------------------------
        // OPTIONAL: NEW EMPLOYEES
        // -------------------------------
        public async Task<List<EmployeeMapping>> GetNewEmployeesAsync()
        {
            try
            {
                var data = await _dbService.GetNewEmployees();

                if (data == null)
                    return new List<EmployeeMapping>();

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching new employees");
                throw;
            }
        }
    }
}

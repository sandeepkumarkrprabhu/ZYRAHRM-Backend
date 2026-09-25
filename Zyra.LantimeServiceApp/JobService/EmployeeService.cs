using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Zyra.LantimeServiceApp.Interfaces;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class EmployeeService : IEmployeeService
    {
        private readonly AttendanceDbContext _context;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(AttendanceDbContext context, ILogger<EmployeeService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<List<EmployeeMapping>> GetActiveEmployees()
        {
            return await _context.EmployeeMappings.Where(f => f.IsActive && !f.IsExcludeFromBiometric).ToListAsync();
        }

        public async Task<List<EmployeeMapping>> GetDirectors()
        {
            return await _context.EmployeeMappings.Where(f => f.IsActive && f.IsExcludeFromBiometric).ToListAsync();
        }

        public async Task<List<EmployeeMapping>> GetEmployeesForSync()
        {
            return await _context.EmployeeMappings.Where(f => f.IsActive && !f.IsExcludeFromBiometric).ToListAsync();
        }

        // NEW METHOD (refactored from Job)
        public async Task<List<string>> GetExistingEmployeeCodes()
        {
            try
            {
                return await _context.EmployeeMappings
                    .AsNoTracking()
                    .Select(e => e.BiometricUserId)
                    .Where(code => code != null && code != "")
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch existing employee codes");
                return new List<string>();
            }
        }

        public async Task<List<EmployeeMapping>?> GetEmployeeByAttendancePolicy(int attendancePolicyId)
        {
            return await (
                            from eap in _context.EmployeeAttendancePolicies
                            join em in _context.EmployeeMappings
                                on eap.EmployeeId equals em.Id
                            where eap.AttendancePolicyId == attendancePolicyId
                                  && eap.IsEnabled
                                  && em.IsActive
                                  && !em.IsExcludeFromBiometric
                            select em
                        ).ToListAsync();
        }

    }
}

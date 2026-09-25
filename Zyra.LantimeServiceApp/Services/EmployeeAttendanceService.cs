using Microsoft.EntityFrameworkCore;
using Zyra.LantimeServiceApp.Interfaces;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class EmployeeAttendanceService : IEmployeeAttendanceService
    {
        private readonly AttendanceDbContext _dbContext;

        public EmployeeAttendanceService(AttendanceDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<Dictionary<string, EmployeeMapperDto>> GetEmployeeMappingsAsync(
            IEnumerable<string> biometricUserIds)
        {
            var ids = biometricUserIds
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<string, EmployeeMapperDto>();

            var employees = await _dbContext.EmployeeMappings
                .AsNoTracking()
                .Where(x =>
                    ids.Contains(x.BiometricUserId) &&
                    !x.IsExcludeFromBiometric &&
                    x.IsActive)
                .Select(x => new EmployeeMapperDto
                {
                    UserId = x.Id,
                    EmployeeCode = x.HRMEmployeeCode,
                    EmployeeName = x.EmployeeName,
                    BiometricUserId = x.BiometricUserId
                })
                .ToListAsync();

            return employees
                .Where(x => !string.IsNullOrWhiteSpace(x.BiometricUserId))
                .GroupBy(x => x.BiometricUserId!)
                .ToDictionary(x => x.Key, x => x.First());
        }

        public async Task<Dictionary<int, EmployeeAttendancePolicy>> GetEmployeeAttendancePoliciesAsync(
            IEnumerable<int> employeeIds)
        {
            var ids = employeeIds
                .Distinct()
                .ToList();

            if (ids.Count == 0)
                return new Dictionary<int, EmployeeAttendancePolicy>();

            var policies = await _dbContext.EmployeeAttendancePolicies
                .AsNoTracking()
                .Include(x => x.AttendancePolicy)
                .ThenInclude(x => x.Rules)
                .Where(x =>
                    ids.Contains(x.EmployeeId) &&
                    x.IsEnabled &&
                    x.AttendancePolicy != null &&
                    x.AttendancePolicy.IsEnable)
                .OrderByDescending(x => x.EffectiveFrom)
                .ToListAsync();

            return policies
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(x => x.Key, x => x.First());
        }
    }
}

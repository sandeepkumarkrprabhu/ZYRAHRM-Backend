using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class EmployeePunchProcessor : IEmployeePunchProcessor
    {
        private readonly AttendanceDbContext _context;
        private readonly ILogger<EmployeePunchProcessor> _logger;

        public EmployeePunchProcessor(
            AttendanceDbContext context,
            ILogger<EmployeePunchProcessor> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ProcessAsync(EmployeeMapping employee, EmployeeMapping biometricData)
        {
            try
            {
                // -----------------------------
                // VALIDATION
                // -----------------------------
                if (employee == null || biometricData == null)
                    return;

                if (string.IsNullOrEmpty(employee.BiometricUserId))
                    return;

                // -----------------------------
                // FETCH DB ENTITY
                // -----------------------------
                var entity = await _context.EmployeeMappings
                    .FirstOrDefaultAsync(x => x.Id == employee.Id);

                if (entity == null)
                {
                    _logger.LogWarning(
                        "Employee not found in DB: {EmpId}",
                        employee.Id);

                    return;
                }

                // -----------------------------
                // CHANGE DETECTION (IMPORTANT)
                // -----------------------------
                if (entity.LatestCheckoutFromBiometric ==
                    biometricData.LatestCheckoutFromBiometric)
                {
                    _logger.LogInformation(
                        "No update needed for Employee {EmpId}",
                        employee.Id);

                    return;
                }

                // -----------------------------
                // UPDATE FIELDS
                // -----------------------------
                entity.LatestCheckoutFromBiometric =
                    biometricData.LatestCheckoutFromBiometric;

                entity.UpdatedUser = "PunchSyncJob";
                entity.UpdatedDateTime = DateTime.Now;

                // -----------------------------
                // SAVE
                // -----------------------------
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Punch updated successfully | Employee: {EmpId} | Device: {DeviceId} | Time: {Time}",
                    employee.Id,
                    employee.BiometricUserId,
                    biometricData.LatestCheckoutFromBiometric);
            }
            catch (Exception ex)
            {
                // IMPORTANT: DO NOT BREAK JOB FLOW
                _logger.LogError(
                    ex,
                    "Failed to update punch for Employee {EmpId}",
                    employee?.Id);
            }
        }
    }
}

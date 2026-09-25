using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using ZYRA.Attendance.Infrastructure;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.JobService
{
    public class EmployeeSyncProcessor : IEmployeeSyncProcessor
    {
        private readonly AttendanceDbContext _context;
        private readonly ILogger<EmployeeSyncProcessor> _logger;

        public EmployeeSyncProcessor(
            AttendanceDbContext context,
            ILogger<EmployeeSyncProcessor> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ProcessAsync(EmployeeMapping employee, PerformContext context)
        {
            try
            {
                var exists = await _context.EmployeeMappings
                    .AnyAsync(x => x.BiometricUserId == employee.BiometricUserId);

                if (exists)
                {
                    _logger.LogInformation("Already exists: {Emp}", employee.BiometricUserId);
                    return;
                }

                await _context.EmployeeMappings.AddAsync(employee);
                await _context.SaveChangesAsync();

                context.WriteLine(
                    ConsoleTextColor.Green,
                    $"Inserted new employee: {employee.EmployeeName}");

                _logger.LogInformation(
                    "Inserted employee {Emp}",
                    employee.BiometricUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed inserting employee {Emp}",
                    employee?.BiometricUserId);
            }
        }
    }
}

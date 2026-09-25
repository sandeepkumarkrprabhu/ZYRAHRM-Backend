
using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using ZYRA.Attendance.Infrastructure;

namespace ZyraHangfireService
{
    public class UserEmployeeService
    {
        private readonly ILogger<UserEmployeeService> _logger;
        private readonly AttendanceDbContext _dbContext;
        private readonly IAttendanceDbService _attendanceDbService;

        public UserEmployeeService(IAttendanceDbService attendanceDbService, AttendanceDbContext dbContext, ILogger<UserEmployeeService> logger)
        {
            _logger = logger;
            _dbContext = dbContext;
            _attendanceDbService = attendanceDbService;
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(600)] // 10 minutes lock
        public async Task SyncNewEmployees(PerformContext context)
        {
            // Step 1: Get employees from biometric DB
            var newEmployees = await _attendanceDbService.GetNewEmployees();

            if (newEmployees == null || !newEmployees.Any())
            {
                var message = $"No new employee records found at {DateTime.Now}";
                _logger.LogInformation(message);
                context.WriteLine(ConsoleTextColor.Black, message);
                return;
            }

            // Step 2: Get existing employee codes from your DB
            var existingCodes = await _dbContext.EmployeeMappings
                .Select(e => e.BiometricUserId)
                .ToListAsync();

            // Step 3: Filter only new employees
            var employeesToInsert = newEmployees
                .Where(e => !existingCodes.Contains(e.BiometricUserId))
                .ToList();

            if (!employeesToInsert.Any())
                return;

            foreach (var employee in employeesToInsert)
            {
                var newUserInfoMessage = $"New employee records found at {DateTime.Now} with code {employee.BiometricUserId}";
                _logger.LogInformation(newUserInfoMessage);
                context.WriteLine(ConsoleTextColor.White, newUserInfoMessage);
            }

            try
            {
                // Step 4: Insert into DB
                await _dbContext.EmployeeMappings.AddRangeAsync(employeesToInsert);
                await _dbContext.SaveChangesAsync();

                var successMessage = $"New employee records updated successfully at {DateTime.Now}";
                _logger.LogInformation(successMessage);
                context.WriteLine(ConsoleTextColor.White, successMessage);
            }
            catch(Exception ex)
            {
                var successMessage = $"New employee records update failed at {DateTime.Now}";
                _logger.LogError(successMessage);
                context.WriteLine(ConsoleTextColor.Red, successMessage);
            }
        }
    }
}

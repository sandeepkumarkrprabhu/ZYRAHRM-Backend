using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;

namespace Zyra.LantimeServiceApp.JobService
{
    public class JobEmployeeMasterSync : IEmployeeSyncJob
    {
        private readonly IAttendanceProvider _attendanceProvider;
        private readonly IEmployeeSyncProcessor _processor;
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<JobEmployeeMasterSync> _logger;

        public JobEmployeeMasterSync(
            IAttendanceProvider attendanceProvider,
            IEmployeeSyncProcessor processor,
            IEmployeeService employeeService,
            ILogger<JobEmployeeMasterSync> logger)
        {
            _attendanceProvider = attendanceProvider;
            _processor = processor;
            _employeeService = employeeService;
            _logger = logger;
        }

        public async Task Execute(PerformContext context)
        {
            try
            {
                _logger.LogInformation("Employee Sync started");

                var newEmployees = await _attendanceProvider.GetNewEmployeesAsync();

                if (newEmployees == null || !newEmployees.Any())
                {
                    context.WriteLine(ConsoleTextColor.Yellow, "No new employees found");
                    return;
                }

                var existingCodes = await _employeeService.GetExistingEmployeeCodes();

                var employeesToInsert = newEmployees
                    .Where(e => !existingCodes.Contains(e.BiometricUserId))
                    .ToList();

                if (employeesToInsert == null || !employeesToInsert.Any())
                {
                    _logger.LogInformation("No new employees to update");
                    context.WriteLine(ConsoleTextColor.Yellow, "No new employees to update");
                    return;
                }


                foreach (var emp in employeesToInsert)
                {
                    await _processor.ProcessAsync(emp, context);
                }

                _logger.LogInformation("Employee Sync completed");
                context.WriteLine(ConsoleTextColor.Green, "Employee sync completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Employee Sync failed");
                context.WriteLine(ConsoleTextColor.Red, ex.Message);
                throw;
            }
        }
    }
}

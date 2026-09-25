using Hangfire;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace ZyraHangfireService
{
    public class AttendanceJobServiceOld
    {
        private readonly ILogger<AttendanceJobService> _logger;
        private readonly ZyraIntegrationCredentials _credentials;
        private readonly IAttendanceDbService _attendanceDbService;
        private readonly IHttpService _httpService;


        public AttendanceJobServiceOld(IOptions<ZyraIntegrationCredentials> options, IAttendanceDbService attendanceDbService, IHttpService httpService, ILogger<AttendanceJobService> logger)
        {
            _credentials = options.Value;
            _attendanceDbService = attendanceDbService;
            _logger = logger;
            _httpService = httpService;
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(600)] // 10 minutes lock
        public async Task SyncCheckInAttendanceAsync(PerformContext? context = null)
        {
            try
            {
                var lastSyncTime = GetLastSyncTime();

                var message = $"Employee Attendance sync started at {DateTime.Now}";
                _logger.LogInformation(message);
                context?.WriteLine(message);

                var records = await _attendanceDbService.GetAttendanceAsync(lastSyncTime);

                message = $"Total employee records fetch from Biometric, {records.Count}";
                _logger.LogInformation(message);
                context?.WriteLine(message);

                EmployeeMapperDto selectedEmployee;
                foreach (var record in records)
                {
                    selectedEmployee = EmployeeCustomCodeMapper.GetPumexEmployeeCode(record.EmployeeCode);
                    if (selectedEmployee != null)
                    {
                        message = $"Attendance sync for the employee {record.EmployeeName}, checkin,  {record.CheckInTime}";
                        _logger.LogInformation(message);
                        context?.WriteLine(message);

                        var apibodyRequest = new AttendanceAPIDto
                        {
                            employee_code = selectedEmployee.EmployeeCode,
                            type = "checkin",
                            date_time = record.CheckInTime,
                        };

                        try
                        {
                            var actionStatus = await SaveAttendance(apibodyRequest, context);
                            if (actionStatus)
                            {
                                message = $"Biometric attendance updated successfully for {record.EmployeeName}";
                                _logger.LogInformation(message);
                                context?.WriteLine(message, ConsoleTextColor.DarkGreen);
                            }
                            else
                            {
                                message = $"Biometric attendance update failed for {record.EmployeeName}";
                                _logger.LogInformation(message);
                                context?.WriteLine(message, ConsoleTextColor.DarkRed);
                            }


                        }
                        catch (Exception ex)
                        {
                            message = $"Biometric attendance updated failed for {record.EmployeeName}";
                            _logger.LogInformation(string.Concat(message, "Error:", ex.StackTrace));
                            context?.WriteLine(ConsoleTextColor.Red, message);
                        }
                    }
                    else
                    {
                        message = $"Biometric attendance updated failed for {record.EmployeeName} lantime code:- {record.EmployeeCode} pumex code {selectedEmployee?.EmployeeCode}";
                        _logger.LogWarning(message);
                        context?.WriteLine(ConsoleTextColor.Yellow, message);
                    }
                }

                //UpdateLastSyncTime(DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance sync failed");
                throw;
            }
        }

        [AutomaticRetry(Attempts = 0)]
        [DisableConcurrentExecution(600)] // 10 minutes lock
        public async Task SyncCheckOutAttendanceAsync(PerformContext? context = null)
        {
            try
            {
                var lastSyncTime = GetLastSyncTime();

                var message = $"Employee Attendnace sync started at {DateTime.Now}";
                _logger.LogInformation(message);
                context?.WriteLine(message);

                var records = await _attendanceDbService.GetAttendanceAsync(lastSyncTime);

                message = $"Total employee records fetch from Biometric, {records.Count}";
                _logger.LogInformation(message);
                context?.WriteLine(message);

                EmployeeMapperDto selectedEmployee;

                foreach (var record in records)
                {
                    selectedEmployee = EmployeeCustomCodeMapper.GetPumexEmployeeCode(record.EmployeeCode);
                    if (selectedEmployee != null)
                    {
                        message = $"Attendnace sync for the employee, {record.EmployeeName}, checkout at ,  {record.CheckOutTime}";
                        _logger.LogInformation(message);
                        context?.WriteLine(message);
                        var apibodyRequest = new AttendanceAPIDto
                        {
                            employee_code = selectedEmployee.EmployeeCode,
                            //type = (DateTime.Now.TimeOfDay >= new TimeSpan(12, 0, 0) ? "checkOut" :"checkin"),
                            type = "checkOut",
                            //date_time = (DateTime.Now.TimeOfDay >= new TimeSpan(12, 0, 0) ? record.CheckOutTime :record.CheckInTime),
                            date_time = record.CheckOutTime,
                        };

                        try
                        {
                            var actionStatus = await SaveAttendance(apibodyRequest, context);

                            if (actionStatus)
                            {
                                message = $"Biometric attendance updated successfully for {record.EmployeeName}";
                                _logger.LogInformation(message);
                                context?.WriteLine(message, ConsoleTextColor.DarkGreen);
                            }
                            else
                            {
                                message = $"Biometric attendance update failed for {record.EmployeeName}";
                                _logger.LogInformation(message);
                                context?.WriteLine(message, ConsoleTextColor.DarkRed);
                            }
                        }
                        catch (Exception ex)
                        {
                            message = $"Biometric attendance updated failed for {record.EmployeeName}";
                            _logger.LogInformation(string.Concat(message, "Error:", ex.StackTrace));
                            context?.WriteLine(ConsoleTextColor.Red, message);
                        }
                    }
                    else
                    {
                        message = $"Biometric attendance updated failed for {record.EmployeeName} lantime code:- {record.EmployeeCode} pumex code {selectedEmployee?.EmployeeCode}";
                        _logger.LogWarning(message);
                        context?.WriteLine(ConsoleTextColor.Yellow, message);
                    }
                }

                //UpdateLastSyncTime(DateTime.Now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Attendance sync failed");
                throw;
            }
        }

        private DateTime GetLastSyncTime()
        {
            return new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day); // Replace with DB value
            //return new DateTime(2026,03,02);
        }

        public async Task<bool> SaveAttendance(AttendanceAPIDto employeeAttendance, PerformContext context)
        {
            var url = new Uri(new Uri("https://pc.pumexinfotech.com/"), "api/attendance/action");

            try
            {
                await _httpService.PostAsync(url.ToString(), employeeAttendance);
                return true;
            }
            catch
            {
                var message = $"Biometric attendance update failed for {employeeAttendance.employee_code} dated {employeeAttendance.date_time}";
                _logger.LogInformation(message);
                context.WriteLine(message);
                return false;
            }
        }

    }
}
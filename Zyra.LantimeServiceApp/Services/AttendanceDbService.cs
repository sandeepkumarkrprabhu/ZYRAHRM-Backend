using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public class AttendanceDbService : IAttendanceDbService
    {
        private readonly ILogger<AttendanceDbService> _logger;
        private readonly IDbService _dbService;

        public AttendanceDbService(
            IDbService dbService,
            ILogger<AttendanceDbService> logger)
        {
            _dbService = dbService;
            _logger = logger;
        }

        public async Task<List<EmployeeMapping>> GetLastPunchTime()
        {
            var query = @"
                SELECT
                    CAST(u.badgenumber AS INT) AS EmployeeCode,
                    MIN(u.name) AS Name,
                    MAX(CONVERT(TIME, c.CheckTime)) AS LastPunchTime
                FROM UserInfo u
                INNER JOIN checkinout c ON u.badgenumber = c.pin
                WHERE u.name <> ''
                  AND c.CheckTime >= DATEADD(MONTH, -3, GETDATE())
                GROUP BY CAST(u.badgenumber AS INT)
                ORDER BY EmployeeCode;";

            return await _dbService.ExecuteQueryAsync(
                query,
                reader =>
                {
                    var code = Convert.ToInt32(reader["EmployeeCode"]);
                    var latestCheckout = new DateTime(1900, 1, 1);

                    if (reader["LastPunchTime"] != DBNull.Value)
                    {
                        var punchTime = (TimeSpan)reader["LastPunchTime"];
                        latestCheckout = latestCheckout.Add(punchTime);
                    }

                    return new EmployeeMapping
                    {
                        BiometricUserId = code.ToString(),
                        EmployeeName = reader["name"]?.ToString(),
                        LatestCheckoutFromBiometric = latestCheckout,
                        UpdatedDateTime = DateTime.Now,
                        UpdatedUser = "Job"
                    };
                },
                parameters: null,
                commandType: CommandType.Text);
        }

        public async Task<List<EmployeeMapping>> GetNewEmployees()
        {
            var query = @"
                SELECT
                    CAST(badgenumber AS INT) AS LantimeCode,
                    name,
                    create_time
                FROM userinfo
                WHERE create_time >= '2026-04-01'";

            return await _dbService.ExecuteQueryAsync(
                query,
                reader =>
                {
                    var code = Convert.ToInt32(reader["LantimeCode"]);

                    return new EmployeeMapping
                    {
                        BiometricUserId = code.ToString(),
                        EmployeeName = reader["name"]?.ToString(),
                        CreatedDateTime = DateTime.Now,
                        CreatedUser = "Job",
                        HRMEmployeeCode = "PU_COC_" + code,
                        IsActive = true,
                        IsExcludeFromBiometric = false,
                        UpdatedDateTime = DateTime.Now,
                        UpdatedUser = "Job"
                    };
                },
                parameters: null,
                commandType: CommandType.Text);
        }

        public async Task<List<BiometricPunch>> GetPunchesAsync(
            DateTime fromDate,
            DateTime toDate)
        {
            var query = @"
                SELECT
                    CAST(c.pin AS NVARCHAR(50)) AS EmployeeCode,
                    c.CheckTime
                FROM checkinout c
                WHERE c.CheckTime >= @FromDate
                  AND c.CheckTime < @ToDate
                ORDER BY c.pin, c.CheckTime;";

            var parameters = new[]
            {
                new SqlParameter("@FromDate", SqlDbType.DateTime)
                {
                    Value = fromDate
                },
                new SqlParameter("@ToDate", SqlDbType.DateTime)
                {
                    Value = toDate
                }
            };

            return await _dbService.ExecuteQueryAsync(
                query,
                reader => new BiometricPunch
                {
                    EmployeeCode = reader["EmployeeCode"]?.ToString() ?? string.Empty,
                    CheckTime = reader.GetDateTime(reader.GetOrdinal("CheckTime"))
                },
                parameters,
                CommandType.Text);
        }

        public async Task<List<AttendanceDto>> GetAttendanceAsync(DateTime fromDate)
        {
            var query = @"
                SELECT
                    CAST(u.badgenumber AS INT) AS [EmployeeCode],
                    u.name AS [EmployeeName],
                    CAST(c.Checktime AS DATE) AS AttendanceDate,
                    MIN(c.Checktime) AS CheckInTime,
                    CASE
                        WHEN MIN(c.Checktime) = MAX(c.Checktime)
                        THEN NULL
                        ELSE MAX(c.Checktime)
                    END AS CheckOutTime
                FROM checkinout c
                JOIN userinfo u ON u.badgenumber = c.pin
                WHERE c.CheckTime >= @FromDate
                  AND c.CheckTime < DATEADD(DAY, 1, @FromDate)
                GROUP BY
                    u.badgenumber,
                    u.name,
                    CAST(c.Checktime AS DATE)
                ORDER BY EmployeeCode ASC";

            var parameters = new[]
            {
                new SqlParameter("@FromDate", SqlDbType.DateTime)
                {
                    Value = fromDate
                }
            };

            return await _dbService.ExecuteQueryAsync(
                query,
                reader => new AttendanceDto
                {
                    EmployeeCode = reader["EmployeeCode"]?.ToString() ?? "",
                    EmployeeName = reader["EmployeeName"]?.ToString() ?? "",
                    CheckInTime = reader.GetDateTime(reader.GetOrdinal("CheckInTime")),
                    CheckOutTime = reader.IsDBNull(reader.GetOrdinal("CheckOutTime"))
                        ? DateTime.MinValue
                        : reader.GetDateTime(reader.GetOrdinal("CheckOutTime"))
                },
                parameters,
                CommandType.Text);
        }
    }
}
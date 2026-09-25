using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceProvider
    {
        /// <summary>
        /// Gets normalized attendance data from biometric source
        /// </summary>
        Task<List<AttendanceDto>> GetAttendanceAsync(DateTime fromDate);

        /// <summary>
        /// Optional: future use for last punch aggregation
        /// </summary>
        Task<List<EmployeeMapping>> GetLastPunchDataAsync();

        /// <summary>
        /// Optional: new employees detected from biometric system
        /// </summary>
        Task<List<EmployeeMapping>> GetNewEmployeesAsync();
    }
}

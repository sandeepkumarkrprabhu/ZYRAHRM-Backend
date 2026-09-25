using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceLogService
    {
        /// <summary>
        /// Logs attendance processing result into database
        /// </summary>
        Task LogAsync(AttendanceDto attendance, bool isSuccess, string attendanceState);
    }
}

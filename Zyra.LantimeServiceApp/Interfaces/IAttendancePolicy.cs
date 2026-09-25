using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendancePolicy
    {
        /// <summary>
        /// Decides whether an attendance action (checkin/checkout) should be created
        /// based on employee, biometric data and current system rules.
        /// </summary>
        AttendanceAPIDto? BuildRequest(EmployeeMapping employee, AttendanceDto attendance, TimeSpan timeSpan);
    }
}

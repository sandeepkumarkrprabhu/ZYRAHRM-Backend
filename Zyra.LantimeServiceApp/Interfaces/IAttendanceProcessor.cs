using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Server;
using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceProcessor
    {
        Task ProcessAsync(
            EmployeeMapping employee,
            AttendanceDto attendance,
            PerformContext context, TimeSpan timeSpan);
    }
}

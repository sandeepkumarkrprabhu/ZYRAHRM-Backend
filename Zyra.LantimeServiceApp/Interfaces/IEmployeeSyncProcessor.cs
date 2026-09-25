using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire.Server;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IEmployeeSyncProcessor
    {
        Task ProcessAsync(EmployeeMapping employee, PerformContext context);
    }
}

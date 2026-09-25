using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IEmployeePunchProcessor
    {
        Task ProcessAsync(EmployeeMapping employee, EmployeeMapping biometricData);
    }
}

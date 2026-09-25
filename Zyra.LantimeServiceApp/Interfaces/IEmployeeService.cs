using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IEmployeeService
    {
        Task<List<EmployeeMapping>> GetEmployeesForSync();
        Task<List<EmployeeMapping>> GetActiveEmployees();
        Task<List<EmployeeMapping>> GetDirectors();

        Task<List<string>> GetExistingEmployeeCodes();

        Task<List<EmployeeMapping>?> GetEmployeeByAttendancePolicy(int attendancePolicyId);
    }
}

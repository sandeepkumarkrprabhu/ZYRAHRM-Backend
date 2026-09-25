using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IEmployeeAttendanceService
    {
        Task<Dictionary<string, EmployeeMapperDto>> GetEmployeeMappingsAsync(
            IEnumerable<string> biometricUserIds);

        Task<Dictionary<int, EmployeeAttendancePolicy>> GetEmployeeAttendancePoliciesAsync(
            IEnumerable<int> employeeIds);
    }
}

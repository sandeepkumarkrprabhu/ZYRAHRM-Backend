using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceDbService
    {
        Task<List<AttendanceDto>> GetAttendanceAsync(DateTime fromDate);
        Task<List<BiometricPunch>> GetPunchesAsync(DateTime fromDate, DateTime toDate);
        Task<List<EmployeeMapping>> GetNewEmployees();
        Task<List<EmployeeMapping>> GetLastPunchTime();
    }
}
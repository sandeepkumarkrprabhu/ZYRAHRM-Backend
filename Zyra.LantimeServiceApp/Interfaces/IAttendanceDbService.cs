using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceDbService
    {
        public Task<List<AttendanceDto>> GetAttendanceAsync(DateTime fromDate);

        public Task<List<EmployeeMapping>> GetNewEmployees();

        public Task<List<EmployeeMapping>> GetLastPunchTime();
    }
}

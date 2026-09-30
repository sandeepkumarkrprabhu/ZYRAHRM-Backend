using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceDbService
    {
        public Task<List<AttendanceDto>> GetAttendanceAsync(DateTime fromDate);

        public Task<List<EmployeeMapping>> GetNewEmployees();

        public Task<List<EmployeeMapping>> GetLastPunchTime();

        public Task<DateTime?> GetLatestPunchAsync(string biometricUserId, DateTime upToTime);

        public Task<DateTime?> GetLatestPunchAfterAsync(string biometricUserId, DateTime checkInTime, DateTime upToTime);
    }
}

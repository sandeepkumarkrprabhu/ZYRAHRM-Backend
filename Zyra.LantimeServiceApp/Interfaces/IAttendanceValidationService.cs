using ZyraHangfireModels.Models;
using ZyraHangfireModels.ServiceModels;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceValidationService
    {
        Task<bool> ValidateCheckInAsync(
            EmployeeMapperDto employee,
            EmployeeAttendancePolicy policy,
            AttendanceDto attendance);

        bool ValidateCheckOut(
            EmployeeMapperDto employee,
            EmployeeAttendancePolicy policy,
            AttendanceDto attendance);
    }
}

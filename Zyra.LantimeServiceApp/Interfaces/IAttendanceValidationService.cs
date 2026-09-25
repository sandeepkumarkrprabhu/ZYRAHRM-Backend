using Zyra.LantimeServiceApp.Models;
using ZyraHangfireModels.Models;
using ZyraHangfireModels.ServiceModels;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceValidationService
    {
        Task<AttendanceValidationResult> ValidateCheckInAsync(
            EmployeeMapperDto employee,
            EmployeeAttendancePolicy policy,
            AttendanceDto attendance);

        AttendanceValidationResult ValidateCheckOut(
            EmployeeMapperDto employee,
            EmployeeAttendancePolicy policy,
            AttendanceDto attendance);
    }
}

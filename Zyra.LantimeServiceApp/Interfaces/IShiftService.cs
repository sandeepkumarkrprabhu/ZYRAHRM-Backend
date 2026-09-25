using ZyraHangfireModels.Models;
using ZyraHangfireModels.ServiceModels;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IShiftService
    {
        ShiftWindow? BuildShiftWindow(
            EmployeeAttendancePolicy employeeShift,
            DateTime referenceTime,
            string employeeName);
    }
}

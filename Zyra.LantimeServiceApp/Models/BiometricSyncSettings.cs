
namespace Zyra.LantimeServiceApp.Models
{
    public class BiometricSyncSettings
    {
        public string AttendanceSyncJobCron { get; set; }

        public string AutoCheckoutJobCron { get; set; }

        public bool CompanyForceCheckoutEnabled { get; set; } = false;

        public string CompanyForceCheckoutJobCron { get; set; } = "59 23 * * *";

        public string DirectorAttendanceJobCron { get; set; }

        public string EmployeeMasterSyncJobCron { get; set; }

        public string EmployeePunchTimeUpdateJobCron { get; set; }
    }
}

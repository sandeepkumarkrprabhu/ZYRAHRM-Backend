
namespace ZyraHangfireModels.PresentationModels
{
    public class EmployeePolicyDetails
    {
        public int EmployeeId { get; set; }

        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int AttendancePolicyId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime EffectiveTo { get; set; }

        public DateTime AutoCheckInTime { get; set; }

        public DateTime AutoCheckOutTime { get; set; }

        public bool IsEnabled { get; set; }

        public DateTime CreatedOn { get; set; }

        public DateTime UpdatedOn { get; set; }
    }
}

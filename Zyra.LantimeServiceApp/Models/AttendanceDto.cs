

using System.Security.Cryptography.X509Certificates;

namespace Zyra.LantimeServiceApp.Models
{
    public class AttendanceDto
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime CheckOutTime { get; set; }
    }
}

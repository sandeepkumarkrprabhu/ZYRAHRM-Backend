namespace Zyra.LantimeServiceApp.Models
{
    public sealed class BiometricPunch
    {
        public string EmployeeCode { get; set; } = string.Empty;
        public DateTime CheckTime { get; set; }
    }
}

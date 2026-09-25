namespace Zyra.LantimeServiceApp.Models
{
    public sealed class AttendanceValidationResult
    {
        public bool IsValid { get; init; }
        public string? Reason { get; init; }
    }
}

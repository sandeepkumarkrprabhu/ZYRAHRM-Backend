namespace ZyraHangfireService
{
    public interface IAttendanceJobEmailNotificationService
    {
        Task NotifyAsync(
            string jobKey,
            string jobName,
            string status,
            DateTime startedAt,
            DateTime completedAt,
            string details,
            CancellationToken cancellationToken = default);
    }
}

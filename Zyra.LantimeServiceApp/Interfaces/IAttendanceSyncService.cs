using Hangfire.Server;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IAttendanceSyncService
    {
        Task SyncCheckInAsync(PerformContext? context = null);
        Task SyncCheckOutAsync(PerformContext? context = null);
    }
}

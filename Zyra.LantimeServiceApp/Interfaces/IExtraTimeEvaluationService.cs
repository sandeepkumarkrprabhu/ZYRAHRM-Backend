using Hangfire.Server;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IExtraTimeEvaluationService
    {
        Task EvaluateAsync(DateTime evaluationTime, PerformContext? context = null);
    }
}

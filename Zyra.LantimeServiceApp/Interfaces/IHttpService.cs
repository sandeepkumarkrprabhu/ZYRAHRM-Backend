
namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IHttpService
    {
        Task PostAsync<T>(string url, T data);
    }
}

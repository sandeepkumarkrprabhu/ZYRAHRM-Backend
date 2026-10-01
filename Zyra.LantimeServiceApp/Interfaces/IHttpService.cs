
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IHttpService
    {
        Task PostAsync<T>(string url, T data);

        Task<HttpApiResult> PostWithResultAsync<T>(string url, T data);
    }
}

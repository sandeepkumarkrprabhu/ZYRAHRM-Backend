namespace Zyra.LantimeServiceApp.Models
{
    public class HttpApiResult
    {
        public bool IsSuccess { get; init; }
        public int StatusCode { get; init; }
        public string ReasonPhrase { get; init; } = string.Empty;
        public string ResponseBody { get; init; } = string.Empty;
    }
}

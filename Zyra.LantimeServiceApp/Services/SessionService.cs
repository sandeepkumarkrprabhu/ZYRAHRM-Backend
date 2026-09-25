using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using Microsoft.Extensions.Caching.Memory;

namespace Zyra.LantimeServiceApp.Services
{
    public class SessionService : ISessionService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ZyraIntegrationCredentials _credentials;

        private const string CACHE_KEY = "ZYRA_SESSION";

        public SessionService(
            HttpClient httpClient,
            IMemoryCache cache,
            IOptions<ZyraIntegrationCredentials> options)
        {
            _httpClient = httpClient;
            _cache = cache;
            _credentials = options.Value;
        }

        public async Task<string> GetTokenAsync()
        {
            if (_cache.TryGetValue(CACHE_KEY, out ZyraSession session))
            {
                if (session.Expiry > DateTime.UtcNow)
                {
                    return session.Token;
                }
            }

            var loginRequest = new
            {
                username = _credentials.Username,
                password = _credentials.Password
            };

            var response = await _httpClient.PostAsJsonAsync("https://pc.pumexinfotech.com/api/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LoginApiResponse>();

            // 🔥 Extract values correctly
            var token = result.Result.Access_Token;
            var expirySeconds = result.Result.Access_Token_Expiry_Time;

            var newSession = new ZyraSession
            {
                Token = token,
                Expiry = DateTime.UtcNow.AddSeconds(expirySeconds - 120)
            };

            _cache.Set(CACHE_KEY, newSession, newSession.Expiry);

            return token;
        }
    }
}

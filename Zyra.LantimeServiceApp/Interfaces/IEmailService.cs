using Microsoft.Extensions.Configuration;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface IEmailService
    {
        Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

        Task SendPasswordEmailAsync(string toEmail, string firstName, string plainPassword, IConfiguration _configuration);
    }
}

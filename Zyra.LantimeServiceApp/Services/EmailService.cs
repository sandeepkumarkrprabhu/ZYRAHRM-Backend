using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Mail;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Exponential backoff: 3 retries (2^1, 2^2, 2^3 seconds)
            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (exception, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception,
                            "Email send attempt {RetryCount} failed. Waiting {Delay} before next retry.",
                            retryCount, timespan);
                    });
        }

        public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var smtpSection = _config.GetSection("Smtp");

            var host = smtpSection["Host"] ?? throw new InvalidOperationException("Smtp:Host is not configured.");
            var port = int.TryParse(smtpSection["Port"], out var parsedPort) ? parsedPort : 587;
            var user = smtpSection["User"] ?? string.Empty;
            var pass = smtpSection["Password"] ?? string.Empty;
            var fromName = smtpSection["FromName"] ?? string.Empty;
            var fromEmail = smtpSection["FromEmail"] ?? throw new InvalidOperationException("Smtp:FromEmail is not configured.");
            var useSsl = bool.TryParse(smtpSection["UseSsl"], out var ssl) ? ssl : true;

            var mail = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = message.Subject ?? string.Empty,
                Body = string.IsNullOrEmpty(message.HtmlBody) ? message.PlainTextBody : message.HtmlBody,
                IsBodyHtml = !string.IsNullOrEmpty(message.HtmlBody)
            };
            mail.To.Add(new MailAddress(message.To, message.ToName ?? string.Empty));
            mail.Bcc.Add(new MailAddress(message.bcc));

            using var smtpClient = new SmtpClient(host, port)
            {
                EnableSsl = useSsl,
                Credentials = string.IsNullOrWhiteSpace(user) ? null : new NetworkCredential(user, pass)
            };

            await _retryPolicy.ExecuteAsync(async (ct) =>
            {
                try
                {
                    await smtpClient.SendMailAsync(mail);
                    _logger.LogInformation("Email sent to {To}", message.To);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending email to {To}", message.To);
                    throw;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        public async Task SendPasswordEmailAsync(string toEmail, string firstName, string plainPassword, IConfiguration configuration = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            var smtp = (configuration ?? _config).GetSection("Smtp");
            var loginUrl = smtp["LoginUrl"] ?? string.Empty;
            var supportEmail = smtp["SupportEmail"] ?? string.Empty;
            var companyName = smtp["FromName"] ?? "Your Company";

            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "User" : firstName.Trim();
            var safePassword = plainPassword ?? string.Empty;

            var plain = $@"Hi {safeFirstName},

Your {companyName} account has been created.

Email: {toEmail}
Temporary password: {safePassword}

Sign in: {loginUrl}

For security, this is a temporary password. You will be required to change your password on first login.
If you did not request this account, contact {supportEmail} immediately.

Regards,
{companyName}";

            var emailMessage = new EmailMessage
            {
                To = toEmail,
                ToName = safeFirstName,
                Subject = $"Your {companyName} account credentials",
                PlainTextBody = plain,
                HtmlBody = null // System.Net.Mail supports HTML, but we keep it simple here
            };

            try
            {
                await SendAsync(emailMessage).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password email to {Email}", toEmail);
                throw;
            }
        }
    }
}

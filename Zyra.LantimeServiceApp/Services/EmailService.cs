using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Mail;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace Zyra.LantimeServiceApp.Services
{
    public sealed class EmailService : IEmailService
    {
        private readonly SmtpOptions _smtp;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;

        public EmailService(
            IOptions<SmtpOptions> smtpOptions,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _smtp = smtpOptions?.Value ?? throw new ArgumentNullException(nameof(smtpOptions));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _retryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    onRetry: (exception, delay, retryCount, _) =>
                        _logger.LogWarning(
                            exception,
                            "Email send attempt {RetryCount} failed. Retrying in {Delay}.",
                            retryCount,
                            delay));
        }

        public async Task SendAsync(
            EmailMessage message,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(message);

            if (string.IsNullOrWhiteSpace(message.To))
                throw new ArgumentException("Email recipient is required.", nameof(message));

            if (string.IsNullOrWhiteSpace(_smtp.Host))
                throw new InvalidOperationException("Smtp:Host is not configured.");

            if (string.IsNullOrWhiteSpace(_smtp.FromEmail))
                throw new InvalidOperationException("Smtp:FromEmail is not configured.");

            using var mail = new MailMessage
            {
                From = new MailAddress(_smtp.FromEmail, _smtp.FromName),
                Subject = message.Subject ?? string.Empty,
                Body = !string.IsNullOrWhiteSpace(message.HtmlBody)
                    ? message.HtmlBody
                    : message.PlainTextBody ?? string.Empty,
                IsBodyHtml = !string.IsNullOrWhiteSpace(message.HtmlBody)
            };

            mail.To.Add(new MailAddress(message.To, message.ToName ?? string.Empty));

            if (!string.IsNullOrWhiteSpace(message.bcc))
            {
                foreach (var address in message.bcc.Split(
                    ';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    mail.Bcc.Add(new MailAddress(address));
                }
            }

            using var smtpClient = new SmtpClient(_smtp.Host, _smtp.Port)
            {
                EnableSsl = _smtp.UseSsl,
                Credentials = string.IsNullOrWhiteSpace(_smtp.User)
                    ? null
                    : new NetworkCredential(_smtp.User, _smtp.Password)
            };

            await _retryPolicy.ExecuteAsync(async ct =>
            {
                ct.ThrowIfCancellationRequested();
                await smtpClient.SendMailAsync(mail, ct).ConfigureAwait(false);
                _logger.LogInformation("Email sent to {To}.", message.To);
            }, cancellationToken).ConfigureAwait(false);
        }

        // Retained for existing callers. SMTP values come from IOptions; the
        // optional configuration parameter supports legacy login/support URL overrides.
        public async Task SendPasswordEmailAsync(
            string toEmail,
            string firstName,
            string plainPassword,
            IConfiguration configuration = null)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new ArgumentException("Recipient email is required.", nameof(toEmail));

            var config = configuration ?? _configuration;
            var loginUrl = config["Smtp:LoginUrl"] ?? _smtp.LoginUrl;
            var supportEmail = config["Smtp:SupportEmail"] ?? _smtp.SupportEmail;
            var companyName = _smtp.FromName;
            var safeFirstName = string.IsNullOrWhiteSpace(firstName) ? "User" : firstName.Trim();
            var safePassword = plainPassword ?? string.Empty;

            var plain = $@"Hi {safeFirstName},

Your {companyName} account has been created.

Email: {toEmail}
Temporary password: {safePassword}

Sign in: {loginUrl}

For security, change your temporary password after signing in.
If you did not request this account, contact {supportEmail}.

Regards,
{companyName}";

            await SendAsync(new EmailMessage
            {
                To = toEmail,
                ToName = safeFirstName,
                Subject = $"Your {companyName} account credentials",
                PlainTextBody = plain
            }).ConfigureAwait(false);
        }
    }
}

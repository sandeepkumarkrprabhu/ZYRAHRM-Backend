using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;

namespace ZyraHangfireService
{
    public sealed class AttendanceJobEmailNotificationService : IAttendanceJobEmailNotificationService
    {
        private readonly IEmailService _emailService;
        private readonly IOptions<AttendanceJobEmailOptions> _options;
        private readonly IOptions<SmtpOptions> _smtpOptions;
        private readonly ILogger<AttendanceJobEmailNotificationService> _logger;

        public AttendanceJobEmailNotificationService(
            IEmailService emailService,
            IOptions<AttendanceJobEmailOptions> options,
            IOptions<SmtpOptions> smtpOptions,
            ILogger<AttendanceJobEmailNotificationService> logger)
        {
            _emailService = emailService;
            _options = options;
            _smtpOptions = smtpOptions;
            _logger = logger;
        }

        public async Task NotifyAsync(
            string jobKey,
            string jobName,
            string status,
            DateTime startedAt,
            DateTime completedAt,
            string details,
            CancellationToken cancellationToken = default)
        {
            var settings = _options.Value;
            var enabled = jobKey switch
            {
                "AttendanceSync" => settings.EnableAttendanceSync,
                "AutoCheckout" => settings.EnableAutoCheckout,
                "CompanyForceCheckout" => settings.EnableCompanyForceCheckout,
                _ => false
            };

            if (!enabled)
            {
                _logger.LogDebug("Email notification disabled for job {JobKey}.", jobKey);
                return;
            }

            var recipients = settings.Recipients
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .SelectMany(x => x.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (recipients.Length == 0)
            {
                _logger.LogWarning(
                    "Email notification for {JobKey} is enabled but no recipients are configured.",
                    jobKey);
                return;
            }

            var template = jobKey switch
            {
                "AttendanceSync" => settings.AttendanceSyncTemplate,
                "AutoCheckout" => settings.AutoCheckoutTemplate,
                "CompanyForceCheckout" => settings.CompanyForceCheckoutTemplate,
                _ => new EmailTemplateOptions()
            };

            var duration = completedAt - startedAt;
            var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["JobName"] = jobName,
                ["Status"] = status,
                ["StartedAt"] = startedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["CompletedAt"] = completedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                ["Duration"] = duration.ToString(@"hh\:mm\:ss"),
                ["Details"] = details,
                ["SupportEmail"] = _smtpOptions.Value.SupportEmail,
                ["LoginUrl"] = _smtpOptions.Value.LoginUrl
            };

            string Render(string source)
            {
                foreach (var token in tokens)
                    source = source.Replace("{" + token.Key + "}", token.Value, StringComparison.OrdinalIgnoreCase);
                return source;
            }

            foreach (var recipient in recipients)
            {
                try
                {
                    await _emailService.SendAsync(new EmailMessage
                    {
                        To = recipient,
                        Subject = Render(template.Subject),
                        PlainTextBody = Render(template.PlainTextBody),
                        HtmlBody = Render(template.HtmlBody)
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Notification failure should be visible but must not mask the job result.
                    _logger.LogError(ex, "Failed to send {JobKey} notification to {Recipient}.", jobKey, recipient);
                }
            }
        }
    }
}

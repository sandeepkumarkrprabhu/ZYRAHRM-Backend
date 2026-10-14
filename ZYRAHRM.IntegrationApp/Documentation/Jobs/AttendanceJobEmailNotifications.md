# Attendance Job Email Notifications

The Hangfire service supports independent email notifications for:
- Attendance Sync
- Attendance Auto Checkout
- Company Force Checkout

## Configuration

Configure these values in the environment-specific application settings file. The sample values in `appsettings.Development.json` are placeholders; use a valid SMTP relay and approved recipients for each environment.

### SMTP

The `Smtp` section is bound to `SmtpOptions` and used by the shared `EmailService`.
- `Host`, `Port`, `UseSsl`: SMTP server settings
- `User`, `Password`: SMTP credentials when authentication is required
- `FromName`, `FromEmail`: sender identity
- `SupportEmail`, `LoginUrl`: optional template tokens for account emails

Store credentials in environment variables or a secret store in deployed environments. Do not commit real SMTP passwords.

### Per-job switches and recipients

Under `AttendanceJobEmailNotifications`:
- `Recipients`: one or more recipient email addresses; each item can also contain semicolon-separated addresses
- `EnableAttendanceSync`: enables/disables Attendance Sync notifications
- `EnableAutoCheckout`: enables/disables Auto Checkout notifications
- `EnableCompanyForceCheckout`: enables/disables Company Force Checkout notifications

All three switches default to `false` in the development sample. Set only the notifications you want to enable to `true`.

### Templates

Each job has its own `Subject`, `HtmlBody`, and `PlainTextBody`: `AttendanceSyncTemplate`, `AutoCheckoutTemplate`, and `CompanyForceCheckoutTemplate`.

Supported placeholders are `{JobName}`, `{Status}`, `{StartedAt}`, `{CompletedAt}`, `{Duration}`, `{Details}`, `{SupportEmail}`, and `{LoginUrl}`.

Templates are configuration-based and bound through `IOptionsMonitor`. The JSON configuration provider reloads file changes when reload-on-change is enabled, so template and switch updates can take effect without restarting. Environment variables and secret-provider changes may require a process restart depending on the provider. Use deployment-specific configuration and never commit real SMTP passwords.

## Behavior

- The email wrapper sends a completion notification when the job completes, and a failure notification if the job throws.
- Email delivery failures are logged and do not mask the original job result.
- Per-employee outcomes are still logged by the underlying job. For jobs that handle individual failures internally, the top-level notification says to review Hangfire logs for employee-level details.
- SMTP settings are shared by all notification templates and existing account emails.
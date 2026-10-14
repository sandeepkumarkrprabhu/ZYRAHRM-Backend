# Attendance Job Email Notifications

The Hangfire service supports independent email notifications for Attendance Sync, Automatic Checkout, and Company Force Checkout. The defaults are defined in `AttendanceJobEmailOptions.cs` and can be overridden in the environment-specific application settings.

## Configuration

Configure these values in the environment-specific application settings file. Sample recipient and SMTP values are placeholders; use an approved SMTP relay and recipients for each environment.

### SMTP

The `Smtp` section is bound to `SmtpOptions` and used by the shared `EmailService`.
- `Host`, `Port`, `UseSsl`: SMTP server settings
- `User`, `Password`: SMTP credentials when authentication is required
- `FromName`, `FromEmail`: sender identity
- `SupportEmail`, `LoginUrl`: optional template tokens for account emails

Store credentials in environment variables or a secret store in deployed environments. Do not commit real SMTP passwords.

### Per-job switches and recipients

Under `AttendanceJobEmailNotifications`:
- `Recipients`: one or more recipient email addresses; each item can contain semicolon-separated addresses.
- `EnableAttendanceSync`: master switch for Attendance Sync notifications.
- `EnableAutoCheckout`: master switch for Automatic Checkout notifications.
- `EnableCompanyForceCheckout`: master switch for Company Force Checkout notifications.

All three switches default to `false` in the development sample. Set a switch to `true` only when that notification is approved for the environment. The Company Force Checkout wrapper checks `EnableCompanyForceCheckout` through the shared notification service; it does not introduce a second switch.

## Default templates

Each notification has its own `Subject`, `HtmlBody`, and `PlainTextBody`:
- `AttendanceSyncTemplate`: completion status, start/end timestamps, duration, and execution details/errors.
- `AutoCheckoutTemplate`: completion status, timestamps, duration, and checkout details.
- `CompanyForceCheckoutTemplate`: one consolidated job summary with successful and failed checkout attempts.

Supported placeholders are `{JobName}`, `{Status}`, `{StartedAt}`, `{CompletedAt}`, `{Duration}`, `{Details}`, `{SupportEmail}`, and `{LoginUrl}`.

The HTML templates place `{Details}` inside a preformatted block so multi-line summaries remain readable. Dynamic token values are HTML-encoded by the notification service. Do not put untrusted HTML into `{Details}`.

## Company Force Checkout summary

After the force-checkout job completes, the wrapper sends one consolidated notification (if enabled). The report is assembled from attendance logs written during the job run and includes:
- Successful checkout count and failed checkout count.
- Employee name and HRM employee code, when the employee mapping is available.
- Biometric device ID.
- The latest successful check-in before the recorded force checkout, when available.
- Checkout timestamp and stored reason.
- Failed attempts in a separate section, including attempt time and recorded reason.

Retries are deduplicated by biometric ID in each outcome section, retaining the latest attempt. A run with failed employee checkout attempts is reported as `Partial`; an unhandled job exception is reported as `Failed`. If no force-checkout attempt is recorded, the notification explains that no employee details were available.

## Runtime behavior

- Notification templates and switches are configuration-based and bound through `IOptionsMonitor`. JSON file changes can take effect without restart when reload-on-change is enabled; other providers may require a restart.
- Email delivery or report-generation failures are logged and must not mask the underlying job result.
- The existing shared SMTP and email service is reused; no separate SMTP implementation is introduced.
- Verify recipient addresses and keep notifications disabled until the SMTP configuration has been tested in the target environment.

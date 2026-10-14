namespace ZyraHangfireService
{
    public sealed class AttendanceJobEmailOptions
    {
        public const string SectionName = "AttendanceJobEmailNotifications";

        public string[] Recipients { get; set; } = Array.Empty<string>();
        public bool EnableAttendanceSync { get; set; }
        public bool EnableAutoCheckout { get; set; }
        public bool EnableCompanyForceCheckout { get; set; }

        public EmailTemplateOptions AttendanceSyncTemplate { get; set; } = new()
        {
            Subject = "[ZYRA HRM] Attendance Sync - {Status}",
            HtmlBody = "<div style='font-family:Segoe UI,Arial,sans-serif;color:#202938;max-width:760px;margin:0 auto'><h2 style='color:#174ea6'>Attendance Sync Summary</h2><p>The attendance synchronization job has completed.</p><table cellpadding='6' cellspacing='0' style='border-collapse:collapse'><tr><td><strong>Status</strong></td><td>{Status}</td></tr><tr><td><strong>Started</strong></td><td>{StartedAt}</td></tr><tr><td><strong>Completed</strong></td><td>{CompletedAt}</td></tr><tr><td><strong>Duration</strong></td><td>{Duration}</td></tr></table><h3>Execution Details</h3><pre style='white-space:pre-wrap;font-family:Segoe UI,Arial,sans-serif;background:#f4f6f8;padding:12px;border-radius:4px'>{Details}</pre><p>Please investigate any reported errors.</p><hr/><small>ZYRA HRM Automated Notifications</small></div>",
            PlainTextBody = "ZYRA HRM - ATTENDANCE SYNC SUMMARY\n\nThe attendance synchronization job has completed.\n\nStatus: {Status}\nStarted At: {StartedAt}\nCompleted At: {CompletedAt}\nDuration: {Duration}\n\nExecution Details:\n{Details}\n\nPlease investigate any reported errors.\n\nZYRA HRM Automated Notifications"
        };

        public EmailTemplateOptions AutoCheckoutTemplate { get; set; } = new()
        {
            Subject = "[ZYRA HRM] Automatic Checkout - {Status}",
            HtmlBody = "<div style='font-family:Segoe UI,Arial,sans-serif;color:#202938;max-width:760px;margin:0 auto'><h2 style='color:#174ea6'>Automatic Checkout Summary</h2><p>The automatic checkout job has completed.</p><table cellpadding='6' cellspacing='0' style='border-collapse:collapse'><tr><td><strong>Status</strong></td><td>{Status}</td></tr><tr><td><strong>Started</strong></td><td>{StartedAt}</td></tr><tr><td><strong>Completed</strong></td><td>{CompletedAt}</td></tr><tr><td><strong>Duration</strong></td><td>{Duration}</td></tr></table><h3>Checkout Details</h3><pre style='white-space:pre-wrap;font-family:Segoe UI,Arial,sans-serif;background:#f4f6f8;padding:12px;border-radius:4px'>{Details}</pre><p>Review failed checkout attempts and verify attendance records where necessary.</p><hr/><small>ZYRA HRM Automated Notifications</small></div>",
            PlainTextBody = "ZYRA HRM - AUTOMATIC CHECKOUT SUMMARY\n\nThe automatic checkout job has completed.\n\nStatus: {Status}\nStarted At: {StartedAt}\nCompleted At: {CompletedAt}\nDuration: {Duration}\n\nCheckout Details:\n{Details}\n\nReview failed checkout attempts and verify attendance records where necessary.\n\nZYRA HRM Automated Notifications"
        };

        public EmailTemplateOptions CompanyForceCheckoutTemplate { get; set; } = new()
        {
            Subject = "[ZYRA HRM] Company Force Checkout Summary - {Status}",
            HtmlBody = "<div style='font-family:Segoe UI,Arial,sans-serif;color:#202938;max-width:900px;margin:0 auto'><h2 style='color:#174ea6'>Company Force Checkout Summary</h2><p>The company force checkout job has completed.</p><table cellpadding='6' cellspacing='0' style='border-collapse:collapse'><tr><td><strong>Status</strong></td><td>{Status}</td></tr><tr><td><strong>Started</strong></td><td>{StartedAt}</td></tr><tr><td><strong>Completed</strong></td><td>{CompletedAt}</td></tr><tr><td><strong>Duration</strong></td><td>{Duration}</td></tr></table><h3>Employee Checkout Results</h3><pre style='white-space:pre-wrap;font-family:Consolas,monospace;background:#f4f6f8;padding:12px;border-radius:4px'>{Details}</pre><p>Only successful checkouts are reported in the successful checkout list. Failed attempts, when present, are listed separately.</p><hr/><small>ZYRA HRM Automated Notifications</small></div>",
            PlainTextBody = "ZYRA HRM - COMPANY FORCE CHECKOUT SUMMARY\n\nThe company force checkout job has completed.\n\nStatus: {Status}\nStarted At: {StartedAt}\nCompleted At: {CompletedAt}\nDuration: {Duration}\n\nEmployee Checkout Results:\n{Details}\n\nOnly successful checkouts are reported in the successful checkout list. Failed attempts, when present, are listed separately.\n\nZYRA HRM Automated Notifications"
        };
    }

    public sealed class EmailTemplateOptions
    {
        public string Subject { get; set; } = "{JobName} - {Status}";
        public string HtmlBody { get; set; } =
            "<h2>{JobName}</h2><p>Status: <strong>{Status}</strong></p><p>Started: {StartedAt}</p><p>Completed: {CompletedAt}</p><p>Duration: {Duration}</p><pre style='white-space:pre-wrap'>{Details}</pre>";
        public string PlainTextBody { get; set; } =
            "{JobName}\nStatus: {Status}\nStarted: {StartedAt}\nCompleted: {CompletedAt}\nDuration: {Duration}\nDetails:\n{Details}";
    }
}

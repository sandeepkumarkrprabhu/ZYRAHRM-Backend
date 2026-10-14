namespace ZyraHangfireService
{
    public sealed class AttendanceJobEmailOptions
    {
        public const string SectionName = "AttendanceJobEmailNotifications";

        public string[] Recipients { get; set; } = Array.Empty<string>();
        public bool EnableAttendanceSync { get; set; }
        public bool EnableAutoCheckout { get; set; }
        public bool EnableCompanyForceCheckout { get; set; }

        public EmailTemplateOptions AttendanceSyncTemplate { get; set; } = new();
        public EmailTemplateOptions AutoCheckoutTemplate { get; set; } = new();
        public EmailTemplateOptions CompanyForceCheckoutTemplate { get; set; } = new();
    }

    public sealed class EmailTemplateOptions
    {
        public string Subject { get; set; } = "{JobName} - {Status}";
        public string HtmlBody { get; set; } =
            "<h2>{JobName}</h2><p>Status: <strong>{Status}</strong></p><p>Started: {StartedAt}</p><p>Completed: {CompletedAt}</p><p>Duration: {Duration}</p><p>Details: {Details}</p>";
        public string PlainTextBody { get; set; } =
            "{JobName}\nStatus: {Status}\nStarted: {StartedAt}\nCompleted: {CompletedAt}\nDuration: {Duration}\nDetails: {Details}";
    }
}

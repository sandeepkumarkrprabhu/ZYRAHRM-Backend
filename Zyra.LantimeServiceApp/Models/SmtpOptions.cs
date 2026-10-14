namespace Zyra.LantimeServiceApp.Models
{
    public sealed class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string User { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromName { get; set; } = "ZYRA HRM";
        public string FromEmail { get; set; } = string.Empty;
        public string SupportEmail { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
    }
}

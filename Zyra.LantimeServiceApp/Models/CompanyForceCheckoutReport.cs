namespace Zyra.LantimeServiceApp.Models
{
    public sealed record CompanyForceCheckoutResult(
        string EmployeeName,
        string EmployeeCode,
        string BiometricDeviceId,
        DateTime? CheckInAt,
        DateTime CheckoutAt,
        string Reason);

    /// <summary>
    /// Collects force-checkout outcomes for one scoped Hangfire job execution.
    /// The wrapper reads this once to send a single consolidated email.
    /// </summary>
    public sealed class CompanyForceCheckoutReport
    {
        private readonly object _sync = new();
        private readonly Dictionary<string, CompanyForceCheckoutResult> _successful =
            new(StringComparer.OrdinalIgnoreCase);
        private readonly List<CompanyForceCheckoutResult> _failed = new();

        public int FailedCount
        {
            get
            {
                lock (_sync)
                    return _failed.Count;
            }
        }

        public void Reset()
        {
            lock (_sync)
            {
                _successful.Clear();
                _failed.Clear();
            }
        }

        public void RecordSuccess(
            string? employeeName,
            string? employeeCode,
            string? biometricDeviceId,
            DateTime? checkInAt,
            DateTime checkoutAt,
            string reason)
        {
            var result = CreateResult(employeeName, employeeCode, biometricDeviceId, checkInAt, checkoutAt, reason);
            var key = !string.IsNullOrWhiteSpace(result.EmployeeCode)
                ? result.EmployeeCode
                : !string.IsNullOrWhiteSpace(result.BiometricDeviceId)
                    ? result.BiometricDeviceId
                    : result.EmployeeName;

            lock (_sync)
            {
                if (!_successful.TryGetValue(key, out var existing) || result.CheckoutAt >= existing.CheckoutAt)
                    _successful[key] = result;
            }
        }

        public void RecordFailure(
            string? employeeName,
            string? employeeCode,
            string? biometricDeviceId,
            DateTime checkoutAt,
            string reason)
        {
            var result = CreateResult(employeeName, employeeCode, biometricDeviceId, null, checkoutAt, reason);
            lock (_sync)
                _failed.Add(result);
        }

        public string BuildDetails()
        {
            lock (_sync)
            {
                var lines = new List<string>
                {
                    $"Successful force checkouts: {_successful.Count}",
                    $"Failed force checkout attempts: {_failed.Count}"
                };

                if (_successful.Count > 0)
                {
                    lines.Add(string.Empty);
                    lines.Add("SUCCESSFUL CHECKOUTS");
                    foreach (var item in _successful.Values.OrderBy(x => x.EmployeeName, StringComparer.OrdinalIgnoreCase))
                    {
                        lines.Add($"Employee Name: {item.EmployeeName}");
                        lines.Add($"Employee Code: {item.EmployeeCode}");
                        lines.Add($"Biometric Device ID: {item.BiometricDeviceId}");
                        lines.Add($"Check-in Timestamp: {(item.CheckInAt.HasValue ? item.CheckInAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "Not available")}");
                        lines.Add($"Checkout Timestamp: {item.CheckoutAt:yyyy-MM-dd HH:mm:ss}");
                        lines.Add($"Reason: {item.Reason}");
                        lines.Add(string.Empty);
                    }
                }

                if (_failed.Count > 0)
                {
                    lines.Add("FAILED CHECKOUT ATTEMPTS");
                    foreach (var item in _failed.OrderBy(x => x.CheckoutAt))
                    {
                        lines.Add($"Employee Name: {item.EmployeeName}");
                        lines.Add($"Employee Code: {item.EmployeeCode}");
                        lines.Add($"Biometric Device ID: {item.BiometricDeviceId}");
                        lines.Add($"Attempt Timestamp: {item.CheckoutAt:yyyy-MM-dd HH:mm:ss}");
                        lines.Add($"Reason: {item.Reason}");
                        lines.Add(string.Empty);
                    }
                }

                if (_successful.Count == 0 && _failed.Count == 0)
                    lines.Add("No force checkout attempts were recorded during this run.");

                return string.Join(Environment.NewLine, lines);
            }
        }

        private static CompanyForceCheckoutResult CreateResult(
            string? employeeName,
            string? employeeCode,
            string? biometricDeviceId,
            DateTime? checkInAt,
            DateTime checkoutAt,
            string? reason)
        {
            return new CompanyForceCheckoutResult(
                string.IsNullOrWhiteSpace(employeeName) ? "Unknown" : employeeName,
                string.IsNullOrWhiteSpace(employeeCode) ? "Not available" : employeeCode,
                string.IsNullOrWhiteSpace(biometricDeviceId) ? "Not available" : biometricDeviceId,
                checkInAt,
                checkoutAt,
                string.IsNullOrWhiteSpace(reason) ? "Company force checkout" : reason);
        }
    }
}

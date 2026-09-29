# Auto Checkout Job

## Purpose
JobAutoCheckoutJob closes open attendance sessions according to configured attendance-policy auto-checkout times. It first checks the biometric device for a later punch. If one exists, that punch is used as checkout; otherwise the policy auto-checkout time is used.

## Flow
Load enabled auto-checkout policies -> find policies whose scheduled checkout is currently due -> get employees assigned to each policy -> evaluate each employee -> biometric checkout or policy checkout.

## Policy Eligibility
Only enabled attendance policies containing the configured shift auto-checkout rule are considered. The scheduled policy checkout must not be in the future and must be within the one-hour eligibility window.

## Employee Eligibility
1. Employee belongs to an eligible attendance policy.
2. HRM employee code and biometric user ID exist.
3. A successful check-in exists.
4. No checkout after that check-in exists.
5. Check-in is not at or after the policy checkout time.

## Biometric Punch Priority
A valid biometric punch after check-in takes precedence over the policy fallback.

Example:
Check-in 09:05, policy checkout 18:00, biometric punch 18:20 -> checkout at 18:20.

If no later biometric punch exists -> checkout at 18:00.

## Overnight Attendance
Attendance logs include the previous calendar day so an overnight open session can remain eligible.

## Database
Successful checkout processing creates an AttendanceLog with employee biometric ID, checkout time, attendance state, processing status, and message.

## Failure Handling
An individual API failure is logged and processing continues for other employees. Unexpected job-level exceptions are rethrown for Hangfire retry/failure handling.

## Logging
Application logger and Hangfire console are used for policy eligibility, employee selection, biometric checkout, policy checkout, skips, and failures.

## Business Rules
1. Policy auto-checkout time is the eligibility boundary.
2. Already-closed attendance is skipped.
3. Checkout must never occur before check-in.
4. A later biometric punch takes precedence over policy fallback.
5. If no later punch exists, policy checkout time is used.
6. Overnight attendance is considered.

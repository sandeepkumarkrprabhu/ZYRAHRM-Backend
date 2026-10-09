# Auto Checkout Job

## Purpose
`JobAutoCheckoutJob` reconciles attendance for employees assigned to enabled attendance policies when the configured auto-checkout time becomes due. The policy time determines when the job evaluates attendance; it is **not** automatically used as an employee's checkout timestamp in this reconciliation path.

## Flow
1. Load enabled policies containing the shift auto-checkout and shift-start rules.
2. Select policies whose configured auto-checkout time is due and within the job's one-hour eligibility window.
3. Load employees assigned to each eligible policy.
4. Read successful attendance logs and determine the latest check-in and checkout for the current attendance session.
5. Follow one of two separate paths:
   - **No successful checkout found:** search for the latest biometric punch after the successful check-in, or after the scheduled shift start when no successful check-in log is available. If a qualifying punch exists, send checkout using that punch's actual timestamp and save a successful attendance log.
   - **Successful checkout already exists:** search for a later biometric punch. If found, process it through `ProcessExtraWorkingTimeAsync` as an extra-working session, with the previous checkout as the extra check-in boundary and the later punch as the extra checkout time.
6. If no qualifying punch is found for an open session, leave it unresolved for the existing Company Force Checkout fallback. Do not invent a checkout timestamp from the job execution time.

## Policy Eligibility
Only enabled attendance policies containing the configured shift auto-checkout rule and a valid shift-start rule are considered. The scheduled policy checkout must not be in the future and must be within the one-hour eligibility window.

## Employee Eligibility
1. The employee has an HRM employee code and biometric user ID.
2. The employee is assigned to an eligible attendance policy.
3. Successful attendance logs are checked from the previous calendar day through the job execution time to account for overnight sessions.
4. The latest successful checkout is evaluated relative to the latest successful check-in when one exists; otherwise the shift-start time is used as the reference boundary.

## Attendance Scenarios

### Open session with a biometric punch
Example: check-in at 09:05 and latest qualifying biometric punch at 18:20. The existing open attendance session is closed at **18:20**, the biometric punch time. The job execution time and configured policy auto-checkout time must not replace this actual punch timestamp.

### Open session without a qualifying biometric punch
No checkout is created in this branch. The session remains unresolved for the existing Company Force Checkout rules.

### Already checked out, then a later biometric punch
Example: normal checkout at 18:30 and a later biometric punch at 20:12. The extra-working-time path handles the interval from 18:30 to 20:12. It must not be used to close an open normal session that has no successful checkout.

### Repeated job execution
The extra-working-time processing checks for existing successful extra check-in/check-out records before creating them. The normal checkout path should also be monitored for duplicate external API submissions if the job is retried after the API succeeds but before the local log is saved.

## Overnight Attendance
Attendance logs include the previous calendar day so an overnight open session can remain eligible. The configured shift-start reference is based on the job's current date; confirm this assumption for shifts that span midnight.

## Database and API
When a qualifying punch closes an open session, the job sends the normal checkout action to the attendance API using the biometric punch timestamp. On API success, it saves an `AttendanceLog` with the biometric employee ID, punch timestamp, checkout state, successful processing status, and an explanatory message.

When a later punch follows an existing successful checkout, `ProcessExtraWorkingTimeAsync` creates the extra check-in/check-out records using the previous checkout and later biometric punch timestamps.

## Failure Handling
If the checkout API call fails, the job logs the failure and leaves the existing Company Force Checkout fallback available. Unexpected job-level exceptions are rethrown so Hangfire can apply configured retry/failure handling.

## Logging
Application logger and Hangfire console record policy eligibility, employee attendance state, reference time, latest biometric punch, checkout results, skipped cases, and failures.

## Business Rules
1. The configured auto-checkout time determines when reconciliation runs; it is not a substitute for an actual biometric checkout time in the open-session punch path.
2. If a successful checkout already exists, only a later biometric punch can trigger extra-working-time processing.
3. If no successful checkout exists, a qualifying punch after the check-in/reference time closes the open session directly; do not call `ProcessExtraWorkingTimeAsync` for this case.
4. If no qualifying punch exists, preserve the existing force-checkout fallback.
5. Never use a punch at or before the reference time as a checkout.
6. Include the previous calendar day when examining logs to support overnight attendance.
7. Keep the open-session and extra-working-time paths separate to avoid creating an unintended extra check-in/check-out pair.

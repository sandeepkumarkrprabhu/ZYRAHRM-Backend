# Auto Checkout Job

## Purpose
`JobAutoCheckoutJob` reconciles an open attendance session when its configured Auto Checkout policy is due. The configured `AUTO_CHECKOUT_TIME` determines when the job runs; it is not used as a fallback employee checkout timestamp.

## Flow
Load enabled auto-checkout policies -> select policies whose scheduled time is due and within the one-hour eligibility window -> get employees assigned to each policy -> find the latest open check-in -> use the latest biometric punch after that check-in as checkout.

## Policy Eligibility
Only enabled attendance policies containing the configured shift auto-checkout and shift-start rules are considered. The scheduled policy checkout must not be in the future and must be within the one-hour eligibility window.

## Employee Processing
1. Employee must have an HRM employee code and biometric user ID.
2. Find the latest successful check-in log within the job's attendance-log lookback window. If no successful check-in log exists, treat the session as a manual check-in and use the configured shift start only as the reference point for searching biometric punches.
3. If a checkout already exists after that check-in and there is no later biometric punch, log the existing check-in/checkout and skip duplicate creation.
4. If a biometric punch exists after the check-in timestamp—or after the shift-start reference when the check-in is inferred as manual—send a checkout to the attendance API using the latest punch timestamp.
5. If no biometric punch exists after the check-in/reference time, skip checkout creation and log the reason. Do not use the policy Auto Checkout time as a fallback.
6. If a checkout already exists and a later punch exists, preserve the existing extra-working-time reconciliation behavior.

## Biometric Punch Priority
The latest biometric punch after the open check-in is the checkout timestamp. If no successful check-in attendance log exists, the job treats the session as manual and searches from shift start because the actual manual check-in time is not available in the attendance logs. Shift start is only a search reference, never the checkout timestamp.

Example:
- Check-in: 2026-10-13 10:29:47
- Biometric punches: 14:48:33, 16:31:28, 17:51:16, 17:51:17
- Checkout timestamp selected: 2026-10-13 17:51:17

If no later biometric punch exists, checkout is skipped. The Company Force Checkout job remains separate and retains its own rules.

## Overnight Attendance
Attendance logs include the previous calendar day so an overnight open session can remain eligible.

## Database and Failure Handling
Successful checkout processing creates an `AttendanceLog` with the employee biometric ID, selected checkout time, `Auto checkout` state, processing status, and message. Failed API attempts are logged with failed status so a subsequent run can retry. An employee-level API failure is logged; unexpected job-level exceptions are rethrown for Hangfire retry/failure handling.

## Logging
Application logs and Hangfire console output include the employee's check-in time, selected/existing checkout time, checkout source, status, and skip/failure reason where applicable.

## Business Rules
1. Configured Auto Checkout time determines job eligibility only.
2. A manual or biometric check-in can start the session.
3. The latest biometric punch after the open check-in is used as checkout.
4. No later biometric punch means checkout is skipped; there is no Auto Checkout-time fallback.
5. Existing checkout records are not duplicated.
6. Company Force Checkout remains separate.

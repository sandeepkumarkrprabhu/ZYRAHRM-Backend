# Director Attendance Job

## Purpose
JobDirectorAttendance creates attendance API entries for directors using the job's synchronization reference time.

## Flow
Get last synchronization time -> get directors -> for each valid director -> check-in at lastSync.Date (00:00) -> checkout at lastSync.Date + 08:30 -> complete.

## Timing Rule
The current implementation uses:
- Check-in = lastSync.Date
- Checkout = lastSync.Date plus 08:30

This is a fixed business rule in the current implementation.

## Employee Eligibility
Directors are obtained from the employee service. A director without an HRM employee code is skipped and logged.

## API Processing
For each valid director:
1. Send check-in API request.
2. Send checkout API request.

## Failure Handling
Individual API failures are logged and processing continues. Unexpected job-level exceptions are rethrown for Hangfire handling.

## Logging
Application logger and Hangfire console record director name, action, success/failure, and attendance timestamp.

## Business Rules
1. Only directors returned by the employee service are processed.
2. Missing HRM codes are skipped.
3. Current attendance window is fixed at 00:00-08:30 based on last synchronization date.
4. Check-in is sent before checkout.
5. API results are logged.

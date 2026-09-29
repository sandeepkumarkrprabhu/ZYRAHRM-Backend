# Hangfire Jobs

Central index for ZYRAHRM background jobs.

## Attendance
- [Attendance Sync](../Attendance/AttendanceSync/README.md)
- [Auto Checkout](../Attendance/AutoCheckout/README.md)
- [Company Force Checkout](../Attendance/CompanyForceCheckout/README.md)
- [Director Attendance](../Attendance/DirectorAttendance/README.md)

## Employee
- [Employee Master Sync](../Employee/EmployeeMasterSync/README.md)
- [Employee Biometric Punch Time Update](../Employee/EmployeeBioPunchTimeUpdate/README.md)

## Job Design Principles
1. Keep jobs primarily as orchestration; keep detailed business rules in services/processors.
2. Do not swallow unexpected exceptions.
3. Continue independent employee processing when the business rule permits it.
4. Make retry/repeated execution safe where applicable.
5. Log enough information to diagnose individual failures.
6. Document external APIs and database changes.
7. Document scheduling and feature-enable conditions.
8. Document edge cases such as overnight shifts and missing biometric data.
9. Avoid N+1 database/API calls for large employee populations.

## Scheduling
Recurring jobs are registered through HangfireJobRegistration and use BiometricSyncSettings. Company Force Checkout additionally depends on the CompanyForceCheckoutEnabled HRM setting.

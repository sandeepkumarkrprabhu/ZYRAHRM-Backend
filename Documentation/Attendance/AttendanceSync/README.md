# Attendance Sync Job

## Purpose
JobSyncAttendanceJob synchronizes attendance check-in and check-out data through the attendance synchronization service.

## Flow
Hangfire -> JobSyncAttendanceJob.Execute -> SyncCheckInAsync -> SyncCheckOutAsync -> completed.

## Business Rules
1. Check-in synchronization runs before check-out synchronization.
2. Detailed synchronization logic belongs to IAttendanceSyncService.
3. Failures propagate to Hangfire.
4. The job must not silently swallow synchronization exceptions.

## Dependencies
- IAttendanceSyncService
- Hangfire PerformContext

## Failure Handling
An exception from either synchronization operation causes the job execution to fail, allowing Hangfire retry/failure handling to apply.

## Performance
Attendance synchronization can involve many biometric records. Prefer incremental and batched processing rather than repeatedly scanning the complete attendance history.

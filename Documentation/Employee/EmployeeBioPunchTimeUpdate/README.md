# Employee Biometric Punch Time Update Job

## Purpose
JobEmployeeBioPunchTimeUpdate synchronizes the latest biometric punch information for active employees.

## Flow
Get active employees + get latest punch data -> build biometric-user-ID lookup -> process each active employee -> complete.

## Matching Rule
Biometric user ID is the matching key. Punch records without a biometric user ID are excluded from the lookup.

## Employee Eligibility
1. Employee is active.
2. Employee has a biometric user ID.
3. A corresponding latest biometric punch exists.

Employees without a punch record are logged and skipped.

## Processing
Matched employee/punch pairs are delegated to IEmployeePunchProcessor.ProcessAsync.

## Synchronization Reference
The job obtains the last synchronization time from IJobService, but the current implementation does not use it as a filter when retrieving latest punch records.

Any future incremental synchronization should explicitly define how this timestamp is applied.

## Failure Handling
Unexpected exceptions are logged and rethrown. Missing punch data for one employee does not stop other employees.

## Logging
Start, missing punch records, successful processing, completion, and failure are logged.

## Business Rules
1. Only active employees are considered.
2. A biometric user ID is required.
3. Punch data is matched by biometric user ID.
4. Missing punch records are skipped.
5. Matched records are delegated to the punch processor.
6. Job-level exceptions remain visible to Hangfire.

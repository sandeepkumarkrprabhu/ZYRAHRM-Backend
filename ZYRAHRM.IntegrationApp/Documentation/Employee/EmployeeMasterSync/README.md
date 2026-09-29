# Employee Master Sync Job

## Purpose
JobEmployeeMasterSync discovers new employees from the biometric provider and processes their employee mappings.

## Flow
Get new employees from biometric provider -> get existing biometric codes -> remove existing employees -> process each new employee -> complete.

## Selection Rule
Only employees whose biometric user ID is not already present in the existing employee-code collection are sent to the synchronization processor.

## Processing
Each new employee is delegated to IEmployeeSyncProcessor.ProcessAsync.

## Failure Handling
A job-level exception is logged and rethrown. An exception escaping an individual processor call currently causes the job to fail.

## Logging
The job logs start, no-new-employee conditions, no-update conditions, completion, and failure. Per-employee details belong to the synchronization processor.

## Idempotency
Existing biometric IDs are filtered before processing, preventing known employees from entering the new-employee path again.

## Business Rules
1. Biometric provider data discovers candidates.
2. Existing biometric IDs are not inserted again.
3. New employees are processed through the employee synchronization processor.
4. Synchronization exceptions remain visible to Hangfire.

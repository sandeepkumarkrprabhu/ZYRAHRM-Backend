# Company Force Checkout

## 1. Purpose

JobCompanyForceCheckout is a background attendance reconciliation job.

The job processes employees whose current time is **outside their applicable active shift**. It first attempts to reconcile the employee's attendance using the latest biometric punch and only uses force checkout as a final fallback when normal attendance processing fails.

The job must process the employee population independently; one employee's failure must not stop processing for other employees.

## 2. High-Level Flow

```text
Job starts
   |
   v
Get current date/time
   |
   v
Determine employees outside applicable active shift
   |
   v
Process each selected employee
   |
   +--> Get latest AttendanceLog state
   |
   +--> Get latest biometric punch
   |
   +--> Reconcile extra time if required
   |
   +--> Checkout open attendance if required
   |
   +--> Collect failures
   |
   v
All selected employees processed
   |
   v
Force checkout failed employees
   |
   v
Log all API results
   |
   v
Job completed
```

## 3. Employee Selection — Shift Exclusion

The first business rule is **shift exclusion**.

Only employees whose current time is outside their applicable active shift are eligible for this job.

### Example

| Shift | Current Time | Processing |
|---|---:|---|
| 09:00–18:00 | 15:30 | Excluded |
| 09:00–18:00 | 18:30 | Process |
| 14:00–22:00 | 18:30 | Excluded |
| 14:00–22:00 | 22:30 | Process |

Overnight shifts must be evaluated correctly.

Example:

```text
Shift: 22:00 → 06:00

23:30 → inside shift → exclude
02:00  → inside shift → exclude
06:30  → outside shift → process
```

Employees inside their active shift must not proceed to attendance reconciliation or force checkout.

## 4. Attendance State Evaluation

For every selected employee, determine:
- Latest attendance check-in.
- Latest attendance checkout.
- Latest biometric punch.

The latest successful attendance records should be used for the decision.

The job should not modify an existing checkout to an earlier biometric punch.

## 5. Existing Checkout + Later Biometric Punch

When a valid attendance checkout already exists and the biometric device contains a later punch:

```text
Attendance checkout = 17:00
Biometric last punch = 18:15
```

The additional period must be reconciled.

### Required API sequence

```text
Check-in API
    Time = 17:00

Checkout API
    Time = 18:15
```

This represents the additional attendance period:

```text
17:00 → 18:15
```

The job must not simply overwrite the original 17:00 checkout.

## 6. Open Check-in

When a check-in exists but no checkout exists:

```text
Check-in = 09:05
Checkout = NULL
Biometric last punch = 18:10
```

Call the checkout API using the latest biometric punch:

```text
Checkout API
    Time = 18:10
```

No additional check-in should be created because an open check-in already exists.

## 7. No Additional Punch

If the latest biometric punch is not later than the recorded checkout:

```text
Attendance checkout = 18:30
Biometric last punch = 18:10
```

No additional attendance period should be created.

The existing checkout remains unchanged.

## 8. Failure Handling

The job must not stop when an individual employee's attendance API operation fails.

Instead:

```text
Employee processing failed
        |
        v
Add employee to failed list
        |
        v
Continue processing next employee
```

After all eligible employees have been processed, the failed employee list is used for the force-checkout fallback.

## 9. Force Checkout Fallback

Force checkout is the **last-resort recovery mechanism**.

It must execute only after the normal reconciliation/checkout processing for all eligible employees has completed.

```text
All employees processed
        |
        v
Failed employee list
        |
        v
Force Checkout API
        |
        v
Record result
```

A successful normal reconciliation or checkout must not subsequently receive a force checkout.

## 10. API Logging

Every attendance API call must be logged, including successful and failed calls.

The log should contain, where available:
- Job name
- Employee ID/code
- Employee name
- API/action
- Attendance state
- Attendance timestamp
- Success/failure
- HTTP/API status
- Response/error message
- Business reason
- Processing timestamp

API results must be available in:
1. Hangfire job log.
2. Application file log.

Example:

```text
JobCompanyForceCheckout
Employee=EMP001
API=Checkout
Time=2026-09-29 18:15:00
Status=SUCCESS
Reason=Open check-in checkout using latest biometric punch
```

## 11. Idempotency

The job may run more than once, so processing must be idempotent.

Before creating an extra check-in or extra checkout, check whether the same successful attendance event has already been recorded.

Repeated execution must not create duplicate attendance periods.

## 12. Important Business Rules

1. Employees inside their active shift are excluded.
2. Employees outside their active shift are eligible for processing.
3. The latest biometric punch is the source for the actual final punch time.
4. A later biometric punch after an existing checkout represents additional attendance time.
5. Additional attendance time is represented by a check-in at the previous checkout and checkout at the latest biometric punch.
6. An open check-in is closed using the latest biometric punch.
7. Existing checkout times must not be moved backward.
8. One employee's API failure must not stop the entire job.
9. Failed employees are collected and force checkout is attempted only after the normal processing pass.
10. Every API result must be logged.
11. Repeated job execution must not create duplicate attendance records.
12. Overnight shifts must be handled correctly.

## 13. Future Considerations

- Employee-specific shift/roster assignment.
- Overnight shifts.
- Multiple shifts or split shifts.
- Missing shift assignment.
- Missing biometric punch.
- Duplicate biometric punches.
- API timeout and transient failures.
- API response/status-body logging.
- Database query batching for large employee populations.
- Client/company-specific attendance rules.

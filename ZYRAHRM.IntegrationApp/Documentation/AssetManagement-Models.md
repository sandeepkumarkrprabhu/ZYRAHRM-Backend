# Asset Management - Initial Models

## Scope

The first asset-management model set is kept separate from existing HRM and biometric models under:

`ZyraHangfireModels/Models/AssetManagement`

Namespace: `ZyraHangfireModels.Models.AssetManagement`

The models currently cover:

- `AssetCategory`: reusable asset categories.
- `Asset`: asset registration and current status.
- `AssetAssignment`: employee assignment and return history.
- `AssetStatusHistory`: historical status transitions.
- `AssetEnums`: ownership type and operational status values.

## Existing employee integration

`AssetAssignment.EmployeeMappingId` is intended to reference the existing `EmployeeMapping.Id`. The asset module does not create a duplicate employee table or modify the biometric employee model.

## Database mapping

The entity attributes map these tables to the SQL Server `asset` schema:

- `asset.AssetCategories`
- `asset.Assets`
- `asset.AssetAssignments`
- `asset.AssetStatusHistory`

The schema and tables are not created until EF Core configurations are registered and a reviewed migration is generated.

## Initial business rules to enforce in the service/configuration layer

1. Asset codes must be unique.
2. An asset must not have more than one active assignment at a time.
3. Returning an asset closes the assignment by setting `ReturnedAt`; it must not delete the assignment record.
4. Status transitions should create a corresponding `AssetStatusHistory` record.
5. Inactive employees must not receive new assignments.
6. Asset assignment, asset status updates, and status-history inserts should be saved atomically.
7. Existing attendance and biometric synchronization behavior must remain unchanged.

## Next steps

1. Add EF Core entity configurations and register the DbSets in `AttendanceDbContext`.
2. Confirm the relationship to `EmployeeMapping` and configure the unique asset-code and active-assignment constraints.
3. Generate and review the migration SQL before applying it to any shared database.
4. Add API/service logic and tests for registration, assignment, return, and concurrent assignment attempts.

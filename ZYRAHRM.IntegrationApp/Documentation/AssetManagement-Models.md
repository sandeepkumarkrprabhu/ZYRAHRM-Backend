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

EF Core configurations are registered through `modelBuilder.ApplyConfigurationsFromAssembly(...)` in the existing `AttendanceDbContext`. Registering the entities does not create database tables by itself; a migration must still be generated and reviewed.

## Initial business rules to enforce in the service/configuration layer

1. Asset codes must be unique.
2. An asset must not have more than one active assignment at a time.
3. Returning an asset closes the assignment by setting `ReturnedAt`; it must not delete the assignment record.
4. Status transitions should create a corresponding `AssetStatusHistory` record.
5. Inactive employees must not receive new assignments.
6. Asset assignment, asset status updates, and status-history inserts should be saved atomically.
7. Existing attendance and biometric synchronization behavior must remain unchanged.

## Next steps

1. **Completed:** Added EF Core configurations under `ZYRA.Attendance.Infrastructure/Configurations/AssetManagement/`.
2. **Completed:** Registered asset DbSets and assembly-based configuration discovery in `AttendanceDbContext`.
3. **Completed:** Configured `AssetAssignment.EmployeeMappingId` as a foreign key to the existing `EmployeeMapping.Id`.
4. **Completed:** Configured a unique asset-code index and SQL Server filtered unique index on active assignments (`ReturnedAt IS NULL`).
5. **Pending validation:** Build the solution and generate/review the migration SQL before applying it to any shared database.
6. Add API/service logic and tests for registration, assignment, return, and concurrent assignment attempts.

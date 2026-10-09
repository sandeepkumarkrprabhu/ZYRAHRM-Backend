# Asset Management — Client Support

## Scope
This change introduces client master data and client-to-employee allocation history. Client allocation is separate from physical laptop assignment.

## Tables
- `asset.ClientMaster`: client code, name, description, active state and audit fields.
- `asset.ClientEmployeeAssignments`: client, existing `EmployeeMapping.Id`, optional project ID, effective dates and remarks.
- `asset.Assets`: physical assets; optional `ClientId` identifies client-owned assets.
- `asset.AssetAssignments`: existing asset-to-employee assignment/return history.

## Business rules
1. `AssetOwnershipType.ClientOwned` is distinct from company-owned (`Owned`) and rented (`Rented`) assets.
2. Client-owned assets require an active ClientId. Company-owned and rented assets must not specify ClientId.
3. Client allocation does not allocate a laptop. AssetAssignments remains the source of truth for laptop usage.
4. Employees can have allocations to different clients. An employee cannot have duplicate open allocations for the same client.
5. End a client allocation by setting EffectiveTo; do not delete history.
6. ProjectId remains nullable until a canonical project master and its ownership are confirmed.
7. Never store device passwords in the database; keep only the secret-vault reference.

## APIs
Client master:
- `GET /api/clients?includeInactive=false`
- `GET /api/clients/{id}`
- `POST /api/clients`
- `PUT /api/clients/{id}`
- `DELETE /api/clients/{id}` (soft deactivation)

Client employee allocations:
- `GET /api/client-employee-assignments?clientId={id}&employeeMappingId={id}`
- `POST /api/client-employee-assignments`
- `POST /api/client-employee-assignments/{id}/end`

Asset API additions:
- Asset write requests accept optional `clientId`.
- Asset responses expose `clientId` and `clientName`.
- `clientId` is required only when `ownershipType` is `ClientOwned`.

## Migration and validation
The EF Core model has changed, so generate and review a migration before applying it:
`dotnet ef migrations add AddClientAssetSupport --project ZYRA.Attendance.Infrastructure --startup-project ZYRAHRM.IntegrationApp`
Then review the generated migration and SQL, build the solution, and test client CRUD, employee allocation/end, and client-owned/company-owned/rented asset registration. Do not apply to a shared database until the migration is reviewed.

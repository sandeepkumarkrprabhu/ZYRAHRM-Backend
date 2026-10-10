# Asset Management — Client Support

## Scope
Client master data and client-to-employee allocation history are managed independently from physical laptop assignments. Client allocation says which client an employee works for; AssetAssignments records which device is issued to an employee.

## Tables
- `asset.ClientMaster`: unique client code, client name, description, active state and audit fields.
- `asset.ClientEmployeeAssignments`: client, existing `EmployeeMapping.Id`, optional project ID, effective dates, remarks and assignment actor.
- `asset.Assets`: physical assets; optional `ClientId` identifies client-owned assets.
- `asset.AssetAssignments`: source of truth for asset issue/return history.

## API — Client master
All routes require authentication.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/clients?includeInactive=false` | List active clients by default; pass `includeInactive=true` to include deactivated clients |
| GET | `/api/clients/{id}` | Get client details |
| POST | `/api/clients` | Create a client |
| PUT | `/api/clients/{id}` | Update client code, name and description |
| DELETE | `/api/clients/{id}` | Soft-deactivate a client; history is retained |

Example create/update body:
```json
{
  "clientCode": "CLIENT-001",
  "clientName": "Example Client Ltd",
  "description": "Optional notes"
}
```

Client code is required and unique. Names are not used as identifiers. A deactivated client cannot be selected for new client-employee allocations or client-owned asset registration.

## API — Client employee allocation
All routes require authentication.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/client-employee-assignments?clientId={id}&employeeMappingId={id}&activeOnly=false` | Filter allocation history; all filters are optional |
| GET | `/api/client-employee-assignments/{id}` | Get a single allocation |
| POST | `/api/client-employee-assignments` | Create an allocation |
| POST | `/api/client-employee-assignments/{id}/end` | End an open allocation while retaining history |

Example allocation body:
```json
{
  "clientId": 1,
  "employeeMappingId": 123,
  "projectId": null,
  "effectiveFrom": "2026-10-09T00:00:00Z",
  "remarks": "Assigned to client project"
}
```

Rules:
1. Client and employee must exist and be active when creating an allocation.
2. One open allocation per client/employee pair is allowed. The same employee can be allocated to different clients.
3. End an allocation by setting `EffectiveTo`; do not delete history.
4. A future-dated allocation cannot be ended before its effective start.
5. `ProjectId` remains optional until a canonical project master and its relationship are confirmed.
6. The allocation endpoint does not issue a laptop. Use AssetAssignments for physical device assignment.

## Asset ownership rules
1. `AssetOwnershipType.ClientOwned` is distinct from company-owned (`Owned`) and rented (`Rented`) assets.
2. Client-owned assets require an active `ClientId`. Company-owned and rented assets must not specify `ClientId`.
3. Asset responses expose `clientId` and `clientName`.
4. Never store device passwords in the database; keep only the secret-vault reference.


## API — Vendor master
All routes require authentication.

| Method | Route | Purpose |
|---|---|---|
| GET | `/api/vendors?includeInactive=false` | List active vendors by default |
| GET | `/api/vendors/{id}` | Get vendor details |
| POST | `/api/vendors` | Register a supplier |
| PUT | `/api/vendors/{id}` | Update supplier/contact details |
| DELETE | `/api/vendors/{id}` | Soft-deactivate a vendor |

Example vendor body:
```json
{
  "vendorCode": "VEND-001",
  "vendorName": "Example IT Suppliers",
  "contactPerson": "Supplier Account Manager",
  "phoneNumber": "+91-0000000000",
  "emailAddress": "accounts@example.com",
  "address": "Supplier billing address",
  "taxRegistrationNumber": "Optional tax registration",
  "notes": "Laptop sales and rentals"
}
```

Vendor codes must be unique. Deactivating a vendor is blocked while it has active rented assets; vendor history for purchased assets is retained.

## Asset procurement and rental details
Assets can optionally reference a vendor using `VendorId`. The asset API also records:
- `ProcurementReference`: purchase order, invoice, or rental agreement reference.
- `RentalStartDate` and `RentalEndDate`: rental period.

Rules:
1. `VendorId` must reference an active vendor when provided.
2. `VendorId` is required for assets whose `OwnershipType` is `Rented`.
3. Rental dates are only accepted for rented assets; end date cannot precede start date.
4. Vendor and client are different relationships. A vendor can supply/rent an asset used by an employee working on a client's project.
5. Vendor information is tracked at asset level for now. Rental charges, payment schedules and accounting are outside the current scope.

## Migration and validation
EF Core model/configuration changes require a migration before deployment. From the solution directory, generate and review the migration:

```bash
dotnet ef migrations add AddVendorAssetTracking --project ZYRA.Attendance.Infrastructure --startup-project ZYRAHRM.IntegrationApp
dotnet build
dotnet test
```

Review the generated migration and SQL before applying it to a shared database. Verify client CRUD, duplicate client-code handling, allocation create/filter/end, duplicate open allocation prevention, and client-owned/company-owned/rented asset registration.

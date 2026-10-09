# Asset Management APIs

## Scope

The first API slice provides CRUD operations for asset categories and asset records. It reuses `AttendanceDbContext` and the asset-management entities in `ZyraHangfireModels.Models.AssetManagement`; it does not add or duplicate employee records.

## Authentication

Both controllers use `[Authorize]` and follow the JWT bearer authentication configured by the IntegrationApp. Include:

```http
Authorization: Bearer <token>
```

## Asset categories

Base route: `/api/asset-categories`

| Method | Route | Behavior |
|---|---|---|
| GET | `/api/asset-categories` | List active categories; `includeInactive=true` includes deactivated categories |
| GET | `/api/asset-categories/{id}` | Get category by ID |
| POST | `/api/asset-categories` | Create category |
| PUT | `/api/asset-categories/{id}` | Update category name and description |
| DELETE | `/api/asset-categories/{id}` | Soft-deactivate category |

Category writes accept `categoryName` (required, 2–100 characters) and optional `description` (up to 500 characters). Category names must be unique. A category with active assets cannot be deactivated.

## Assets

Base route: `/api/assets`

| Method | Route | Behavior |
|---|---|---|
| GET | `/api/assets` | Filtered, paginated list |
| GET | `/api/assets/{id}` | Get asset by ID |
| POST | `/api/assets` | Register an asset |
| PUT | `/api/assets/{id}` | Update asset details |
| DELETE | `/api/assets/{id}` | Soft-deactivate asset |

List query parameters:

- `pageNumber`: starts at 1; default 1.
- `pageSize`: default 20; maximum 100.
- `search`: matches asset code, name, serial number, or manufacturer.
- `categoryId`: filter by category.
- `status`: filter by numeric `AssetStatus` enum value.
- `includeInactive`: default false.

Asset create/update requests include asset code, name, category ID, ownership type, optional descriptive/purchase/location fields, and optional device administrator account metadata. The API checks that the category exists and is active, validates ownership enum values, and checks asset-code uniqueness. New assets start with `Available` status; clients cannot set the status through this general-purpose write request because status transitions should be handled with assignment/maintenance workflows and status history.

Assets with an active assignment or `Assigned` status cannot be deactivated. Delete endpoints are soft-delete operations: records and historical references remain in the database.

## Device administrator account metadata

Asset registration/update can record `deviceAdminAccountName`, `deviceAdminCredentialSecretReference`, and `deviceAdminAccountNotes`. The username is the Microsoft/device administrator account associated with the laptop. `deviceAdminCredentialSecretReference` must be an identifier or URI for a secret held in an approved secret manager (for example, Azure Key Vault); it is not the password itself. **Do not send or persist raw passwords in the asset API/database.** The normal asset response returns `hasDeviceAdminCredentialReference` instead of the reference value, and never returns any password. Secret vault access policies and credential rotation must be configured separately. If the team has not provisioned a secret vault yet, leave the reference empty rather than storing a password in plain text.

Example additional asset request fields:

```json
{
  "deviceAdminAccountName": "device-admin@company.example",
  "deviceAdminCredentialSecretReference": "asset-admin-credentials/LAP-0001",
  "deviceAdminAccountNotes": "Company-managed Microsoft account"
}
```

## Response and error behavior

- `200 OK`: successful read or update.
- `201 Created`: successful create, with a location pointing to the new resource.
- `204 No Content`: successful soft-deactivation.
- `400 Bad Request`: invalid query values or request references.
- `401 Unauthorized`: missing/invalid bearer token.
- `404 Not Found`: resource ID does not exist.
- `409 Conflict`: duplicate name/code or prohibited deactivation.
- `500 Internal Server Error`: unexpected server failures handled by the application's global error behavior.

Asset list responses contain `items`, `pageNumber`, `pageSize`, `totalCount`, and `totalPages`. Asset response DTOs include the category name and avoid returning EF entities/navigation graphs directly.

## Example requests

Create category:

```http
POST /api/asset-categories
Content-Type: application/json
Authorization: Bearer <token>

{
  "categoryName": "Laptop",
  "description": "Employee laptops"
}
```

Create asset:

```http
POST /api/assets
Content-Type: application/json
Authorization: Bearer <token>

{
  "assetCode": "LAP-0001",
  "assetName": "Development Laptop",
  "assetCategoryId": 1,
  "manufacturer": "Dell",
  "modelNumber": "Latitude",
  "ownershipType": 1,
  "location": "Kochi Office"
}
```

List assets:

```http
GET /api/assets?pageNumber=1&pageSize=20&categoryId=1&search=LAP
Authorization: Bearer <token>
```

## Validation status and follow-up

The files have been committed to the feature branch, but the remote repository integration does not run a local .NET build or automated tests. Build and test the solution before merging. Add automated tests for duplicate values, inactive categories, pagination boundaries, authorization, and deactivation rules. Assignment/return endpoints and explicit status-history workflows are a separate next step.

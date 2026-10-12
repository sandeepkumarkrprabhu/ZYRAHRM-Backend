# Module-aware navigation and RBAC

## Design
The existing `Roles`, `Permissions`, and `RolePermissions` tables remain in place. The `Modules` master groups navigation menus. `NavigationMenus.ModuleId` associates each menu with its module. The menu's `RequiredPermissionCode` is resolved through the existing Permissions and RolePermissions records.

This avoids introducing duplicate module-permission, role-menu, or user-role tables. Module grouping does not replace endpoint-level authorization.

## Seed module catalog
- `HRM` — Human Resource Management
- `ATTENDANCE` — Attendance policies, biometric synchronization, attendance APIs, and background jobs
- `ADMIN` — Administration, audit, and system configuration
- `ASSET` — Asset Management

## Asset Management navigation
| Menu key | Label | Required permission |
|---|---|---|
| `asset-cat` | Asset Categories | `ASSET_CATEGORY_VIEW` |
| `assets` | Asset Master | `ASSET_VIEW` |
| `clients` | Client Master | `CLIENT_VIEW` |
| `vendors` | Vendor Master | `VENDOR_VIEW` |

All four menus map to the `ASSET` module. Menu IDs 9–12 are used in the current seed.

## Asset permissions
- `ASSET_CATEGORY_VIEW`, `ASSET_CATEGORY_MANAGE`
- `ASSET_VIEW`, `ASSET_MANAGE`
- `CLIENT_VIEW`, `CLIENT_MANAGE`
- `VENDOR_VIEW`, `VENDOR_MANAGE`

The navigation menu uses a view permission so that users with read-only access can see a menu. Write operations must independently check the matching manage permission on the API. Do not treat menu visibility as API security.

## Asset role grants seeded
- **Super Admin:** all eight asset permissions.
- **HR Admin:** all eight asset permissions.
- **IT Admin:** all eight asset permissions.
- **Asset Manager:** all eight asset permissions.
- **Auditor:** view-only permissions for categories, assets, clients, and vendors.
- Existing Attendance Manager grants are unchanged; no Asset Management access is added by this seed.

These are the initial seed grants. Review business policy before production; in particular, limit data visibility and assignment actions by client/project scope where required.

## Existing menu mapping
- Dashboard, Employees → HRM
- Attendance Policies, Hangfire & ZYRA API → Attendance
- User Access & HR, Audit Logs, Settings → Administration

## Attendance and Integration consolidation
Attendance is the single module for attendance policy management, biometric synchronization, attendance APIs, and the Hangfire jobs that process attendance. There is no separate Integration module because the integration work currently serves Attendance only. The existing `SYNC_TRIGGER` permission is retained to avoid breaking role grants and API contracts; its category and description now identify it as an Attendance permission.

The `Hangfire & ZYRA API` navigation menu (MenuId 5) is mapped to `ATTENDANCE` (ModuleId 2). The obsolete `INTEGRATION` module seed (ModuleId 3) is removed. Existing deployments must apply a reviewed EF Core migration that updates the menu foreign key before deleting the old module row. Do not delete ModuleId 3 before moving any existing menus that still reference it.

## Migration note
Module, navigation, permission, and role-permission records are seeded via EF Core `HasData`. Existing installations must apply a reviewed EF Core migration for these seed changes. Verify the generated migration uses insert/update/delete operations safely for existing data and does not conflict with manually assigned IDs. `NavigationMenus.ModuleId` is nullable during rollout; backfill legacy rows before making it non-nullable.

## API/navigation behavior
The authorized navigation response includes `moduleId`, `moduleCode`, and `moduleName`. Menus are filtered using the existing permission and role-permission joins. Module assignment groups navigation; it does not replace endpoint-level permission checks.

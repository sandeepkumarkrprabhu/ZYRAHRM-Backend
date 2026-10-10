# Module-aware navigation and RBAC

## Design
The existing `Roles`, `Permissions`, and `RolePermissions` tables remain in place. A new `Modules` master table groups navigation menus. Each `NavigationMenus` record has a required `ModuleId` foreign key. Permissions remain linked to menus by `RequiredPermissionCode`, and role authorization continues to use `RolePermissions`.

This avoids introducing duplicate module-permission, role-menu, or user-role tables.

## Seed module catalog
- `HRM` — Human Resource Management
- `ATTENDANCE` — Attendance Management
- `INTEGRATION` — Biometric and integration operations
- `ADMIN` — Administration, audit, and system configuration

## Existing menu mapping
- Dashboard, Employees, User Access & HR → HRM
- Attendance Policies → Attendance
- Hangfire & ZYRA API → Integration
- Audit Logs, Settings → Administration

## Migration note
Generate an EF Core migration after pulling this branch. Because existing databases already contain navigation menus, the migration must create and seed `Modules`, populate `NavigationMenus.ModuleId` for existing rows, and only then make `ModuleId` non-nullable. Do not apply a migration that adds a required foreign key before backfilling existing rows. Review the generated migration and model snapshot before updating the database.

## API/navigation behavior
The navigation response includes `moduleId`, `moduleCode`, and `moduleName`. Menus remain filtered using the existing permission and role-permission joins. Module assignment groups navigation; it does not replace endpoint-level permission checks.

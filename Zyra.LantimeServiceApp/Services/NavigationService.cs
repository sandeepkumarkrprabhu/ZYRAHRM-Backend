using Microsoft.EntityFrameworkCore;
using ZYRA.Attendance.Infrastructure;
using Zyra.LantimeServiceApp.Interfaces;
using Zyra.LantimeServiceApp.Models;
using Microsoft.Extensions.Logging;

namespace Zyra.LantimeServiceApp.Services
{
    public class NavigationService : INavigationService
    {
        private readonly AttendanceDbContext _dbContext;
        private readonly ILogger<NavigationService> _logger;

        public NavigationService(
            AttendanceDbContext dbContext,
            ILogger<NavigationService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<List<NavigationMenuDto>> GetMenusByUserIdAsync(long userId)
        {
            var userRoleName = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == userId && u.Status && !u.IsLocked)
                .Select(u => u.RoleName)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(userRoleName))
            {
                _logger.LogWarning(
                    "Navigation menu request failed. Active user {UserId} was not found.",
                    userId);

                return new List<NavigationMenuDto>();
            }

            var menus = await (
                from menu in _dbContext.NavigationMenus.AsNoTracking()
                join permission in _dbContext.Permissions.AsNoTracking()
                    on menu.RequiredPermissionCode equals permission.PermissionCode
                join rolePermission in _dbContext.RolePermissions.AsNoTracking()
                    on permission.Id equals rolePermission.PermissionId
                join role in _dbContext.Roles.AsNoTracking()
                    on rolePermission.RoleId equals role.Id
                where role.RoleName == userRoleName
                orderby menu.DisplayOrder
                select new NavigationMenuDto
                {
                    MenuId = menu.MenuId,
                    MenuKey = menu.MenuKey,
                    MenuLabel = menu.MenuLabel,
                    IconName = menu.IconName,
                    DisplayOrder = menu.DisplayOrder,
                    RequiredPermissionCode = menu.RequiredPermissionCode,
                    Description = menu.Description
                })
                .Distinct()
                .ToListAsync();

            _logger.LogInformation(
                "Fetched {Count} navigation menus for user {UserId} with role {RoleName}.",
                menus.Count,
                userId,
                userRoleName);

            return menus;
        }
    }
}

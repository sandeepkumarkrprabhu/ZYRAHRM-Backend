namespace Zyra.LantimeServiceApp.Interfaces
{
    public interface INavigationService
    {
        Task<List<Models.NavigationMenuDto>> GetMenusByUserIdAsync(long userId);
    }
}

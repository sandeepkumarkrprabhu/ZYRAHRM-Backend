namespace Zyra.LantimeServiceApp.Models
{
    public class NavigationMenuDto
    {
        public int MenuId { get; set; }
        public string MenuKey { get; set; }
        public string MenuLabel { get; set; }
        public string IconName { get; set; }
        public int DisplayOrder { get; set; }
        public string RequiredPermissionCode { get; set; }
        public string Description { get; set; }
    }
}

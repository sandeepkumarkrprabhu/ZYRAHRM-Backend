namespace Zyra.LantimeServiceApp.Models
{
    public class NavigationMenuDto
    {
        public int MenuId { get; set; }
        public int? ModuleId { get; set; }
        public string ModuleCode { get; set; }
        public string ModuleName { get; set; }
        public string MenuKey { get; set; }
        public string MenuLabel { get; set; }
        public string IconName { get; set; }
        public int DisplayOrder { get; set; }
        public string RequiredPermissionCode { get; set; }
        public string Description { get; set; }
    }
}

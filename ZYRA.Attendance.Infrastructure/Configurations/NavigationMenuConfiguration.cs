using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models;

namespace ZYRA.Attendance.Infrastructure.Configurations
{
    public class NavigationMenuConfiguration : IEntityTypeConfiguration<NavigationMenus>
    {
        public void Configure(EntityTypeBuilder<NavigationMenus> builder)
        {
            builder.HasOne<Modules>()
                .WithMany()
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ModuleId);
        }
    }
}

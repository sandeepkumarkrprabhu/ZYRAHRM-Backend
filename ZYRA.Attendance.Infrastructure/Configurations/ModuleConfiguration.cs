using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ZyraHangfireModels.Models;

namespace ZYRA.Attendance.Infrastructure.Configurations
{
    public class ModuleConfiguration : IEntityTypeConfiguration<Modules>
    {
        public void Configure(EntityTypeBuilder<Modules> builder)
        {
            builder.ToTable("Modules");
            builder.HasKey(x => x.ModuleId);
            builder.Property(x => x.ModuleCode).HasMaxLength(50).IsRequired();
            builder.Property(x => x.ModuleName).HasMaxLength(100).IsRequired();
            builder.Property(x => x.Description).HasMaxLength(250);
            builder.HasIndex(x => x.ModuleCode).IsUnique();
        }
    }
}

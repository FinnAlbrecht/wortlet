using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wortlet.Domain.Recognition;
using Wortlet.Infrastructure.Identity;

namespace Wortlet.Infrastructure.Persistence.Configurations;

internal sealed class RecognitionUsageConfiguration : IEntityTypeConfiguration<RecognitionUsage>
{
    public void Configure(EntityTypeBuilder<RecognitionUsage> b)
    {
        b.ToTable("RecognitionUsages");
        b.HasKey(u => u.Id);
        b.Property(u => u.Id).ValueGeneratedNever();
        b.HasIndex(u => new { u.OwnerId, u.Day }).IsUnique();
        b.HasOne<AppUser>().WithMany().HasForeignKey(u => u.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}

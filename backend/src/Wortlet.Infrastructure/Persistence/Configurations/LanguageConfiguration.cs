using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wortlet.Domain.Languages;
using Wortlet.Infrastructure.Identity;

namespace Wortlet.Infrastructure.Persistence.Configurations;

internal sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> b)
    {
        b.ToTable("Languages");
        b.HasKey(l => l.Id);
        b.Property(l => l.Id).ValueGeneratedNever();
        b.Property(l => l.Name).HasMaxLength(Language.MaxNameLength).IsRequired();
        b.HasIndex(l => l.OwnerId);
        b.HasOne<AppUser>().WithMany().HasForeignKey(l => l.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}

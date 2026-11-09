using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Groups;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Profiles;
using Wortlet.Domain.Sets;
using Wortlet.Domain.Statistics;
using Wortlet.Infrastructure.Identity;

namespace Wortlet.Infrastructure.Persistence.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> b)
    {
        b.ToTable("Profiles");
        b.HasKey(p => p.Id);
        b.Property(p => p.Id).ValueGeneratedNever();
        b.Property(p => p.DisplayName).HasMaxLength(UserProfile.MaxNameLength).IsRequired();
        b.Property(p => p.AvatarIcon).HasMaxLength(16).IsRequired();
        b.Property(p => p.AvatarColor).HasMaxLength(16).IsRequired();
        b.HasOne<AppUser>().WithOne().HasForeignKey<UserProfile>(p => p.Id).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AvatarImageConfiguration : IEntityTypeConfiguration<AvatarImage>
{
    public void Configure(EntityTypeBuilder<AvatarImage> b)
    {
        b.ToTable("AvatarImages");
        b.HasKey(i => i.Id);
        b.Property(i => i.Id).ValueGeneratedNever();
        b.Property(i => i.ContentType).HasMaxLength(32).IsRequired();
        b.Property(i => i.Data).IsRequired();
        b.HasOne<AppUser>().WithOne().HasForeignKey<AvatarImage>(i => i.Id).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class WordStatConfiguration : IEntityTypeConfiguration<WordStat>
{
    public void Configure(EntityTypeBuilder<WordStat> b)
    {
        b.ToTable("WordStats");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.WordKey).HasMaxLength(Word.MaxKeyLength).IsRequired();
        b.HasIndex(s => new { s.FolderId, s.WordKey }).IsUnique();
        b.HasIndex(s => s.OwnerId);
        b.HasOne<Folder>().WithMany().HasForeignKey(s => s.FolderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PracticeDayConfiguration : IEntityTypeConfiguration<PracticeDay>
{
    public void Configure(EntityTypeBuilder<PracticeDay> b)
    {
        b.ToTable("PracticeDays");
        b.HasKey(d => d.Id);
        b.Property(d => d.Id).ValueGeneratedNever();
        b.HasIndex(d => new { d.OwnerId, d.Day }).IsUnique();
        b.HasOne<AppUser>().WithMany().HasForeignKey(d => d.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuizResultConfiguration : IEntityTypeConfiguration<QuizResult>
{
    public void Configure(EntityTypeBuilder<QuizResult> b)
    {
        b.ToTable("QuizResults");
        b.HasKey(r => r.Id);
        b.Property(r => r.Id).ValueGeneratedNever();
        b.Property(r => r.Title).HasMaxLength(QuizResult.MaxTitleLength).IsRequired();
        b.Property(r => r.Direction).HasMaxLength(8).IsRequired();
        b.HasIndex(r => new { r.OwnerId, r.FinishedAt });
        b.HasOne<AppUser>().WithMany().HasForeignKey(r => r.OwnerId).OnDelete(DeleteBehavior.Cascade);
        // Die Note bleibt im Verlauf, auch wenn Ordner oder Set gelöscht werden.
        b.HasOne<Folder>().WithMany().HasForeignKey(r => r.FolderId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne<WordSet>().WithMany().HasForeignKey(r => r.SetId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class WordSetConfiguration : IEntityTypeConfiguration<WordSet>
{
    public void Configure(EntityTypeBuilder<WordSet> b)
    {
        b.ToTable("WordSets");
        b.HasKey(s => s.Id);
        b.Property(s => s.Id).ValueGeneratedNever();
        b.Property(s => s.Name).HasMaxLength(WordSet.MaxNameLength).IsRequired();
        b.HasIndex(s => new { s.OwnerId, s.LanguageId });
        b.HasOne<AppUser>().WithMany().HasForeignKey(s => s.OwnerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Language>().WithMany().HasForeignKey(s => s.LanguageId).OnDelete(DeleteBehavior.Cascade);

        b.OwnsMany(s => s.Items, i =>
        {
            i.ToTable("WordSetItems");
            i.WithOwner().HasForeignKey("WordSetId");
            i.Property<Guid>("WordSetId");
            i.Property(x => x.WordKey).HasMaxLength(Word.MaxKeyLength).IsRequired();
            i.HasKey("WordSetId", nameof(SetItem.FolderId), nameof(SetItem.WordKey));
            // Wird ein Ordner gelöscht, verschwinden seine Wörter aus allen Sets.
            i.HasOne<Folder>().WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Navigation(s => s.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> b)
    {
        b.ToTable("Groups");
        b.HasKey(g => g.Id);
        b.Property(g => g.Id).ValueGeneratedNever();
        b.Property(g => g.Name).HasMaxLength(Group.MaxNameLength).IsRequired();
        b.Property(g => g.JoinCode).HasMaxLength(Group.CodeLength).IsRequired();
        b.HasIndex(g => g.JoinCode).IsUnique();
        b.Property(g => g.Visibility).HasConversion<string>().HasMaxLength(16);
        b.HasIndex(g => g.Visibility);

        b.OwnsMany(g => g.JoinRequests, r =>
        {
            r.ToTable("GroupJoinRequests");
            r.WithOwner().HasForeignKey("GroupId");
            r.Property<Guid>("GroupId");
            r.HasKey("GroupId", nameof(GroupJoinRequest.UserId));
            r.HasIndex(x => x.UserId);
            r.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Navigation(g => g.JoinRequests).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(g => g.Members, m =>
        {
            m.ToTable("GroupMembers");
            m.WithOwner().HasForeignKey("GroupId");
            m.Property<Guid>("GroupId");
            m.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
            m.HasKey("GroupId", nameof(GroupMember.UserId));
            m.HasIndex(x => x.UserId);
            m.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Navigation(g => g.Members).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(g => g.Shares, s =>
        {
            s.ToTable("GroupShares");
            s.WithOwner().HasForeignKey("GroupId");
            s.Property<Guid>("GroupId");
            s.HasKey(x => x.Id);
            s.Property(x => x.Id).ValueGeneratedNever();
            // Löscht die Leitung den Ordner bzw. das Set, verschwindet auch die Freigabe.
            s.HasOne<Folder>().WithMany().HasForeignKey(x => x.FolderId).OnDelete(DeleteBehavior.Cascade);
            s.HasOne<WordSet>().WithMany().HasForeignKey(x => x.SetId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Navigation(g => g.Shares).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

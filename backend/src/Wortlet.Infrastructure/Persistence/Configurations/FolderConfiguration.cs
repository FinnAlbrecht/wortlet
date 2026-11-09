using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;
using Wortlet.Infrastructure.Identity;

namespace Wortlet.Infrastructure.Persistence.Configurations;

internal sealed class FolderConfiguration : IEntityTypeConfiguration<Folder>
{
    public void Configure(EntityTypeBuilder<Folder> b)
    {
        b.ToTable("Folders");
        b.HasKey(f => f.Id);
        b.Property(f => f.Id).ValueGeneratedNever();
        b.Property(f => f.Name).HasMaxLength(Folder.MaxNameLength).IsRequired();
        b.Property(f => f.Description).HasMaxLength(Folder.MaxDescriptionLength).IsRequired();
        b.Property(f => f.OldLabel).HasMaxLength(Folder.MaxLabelLength).IsRequired();
        b.Property(f => f.NewLabel).HasMaxLength(Folder.MaxLabelLength).IsRequired();
        b.Ignore(f => f.AllWords);
        b.Ignore(f => f.WordCount);

        b.HasIndex(f => new { f.OwnerId, f.LanguageId });
        b.HasOne<AppUser>().WithMany().HasForeignKey(f => f.OwnerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Language>().WithMany().HasForeignKey(f => f.LanguageId).OnDelete(DeleteBehavior.NoAction);

        b.HasMany(f => f.Chapters).WithOne().HasForeignKey(c => c.FolderId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(f => f.Chapters).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(f => f.WeakWords, w =>
        {
            w.ToTable("WeakWords");
            w.WithOwner().HasForeignKey("FolderId");
            w.Property<Guid>("FolderId");
            w.Property(x => x.WordKey).HasMaxLength(Word.MaxKeyLength).IsRequired();
            w.HasKey("FolderId", nameof(WeakWord.WordKey));
        });
        b.Navigation(f => f.WeakWords).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ChapterConfiguration : IEntityTypeConfiguration<Chapter>
{
    public void Configure(EntityTypeBuilder<Chapter> b)
    {
        b.ToTable("Chapters");
        b.HasKey(c => c.Id);
        b.Property(c => c.Id).ValueGeneratedNever();
        b.Property(c => c.Name).HasMaxLength(Chapter.MaxNameLength).IsRequired();
        b.Property(c => c.Pages).HasMaxLength(Chapter.MaxPagesLength).IsRequired();
        b.HasMany(c => c.Words).WithOne().HasForeignKey(w => w.ChapterId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(c => c.Words).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class WordConfiguration : IEntityTypeConfiguration<Word>
{
    public void Configure(EntityTypeBuilder<Word> b)
    {
        b.ToTable("Words");
        b.HasKey(w => w.Id);
        b.Property(w => w.Id).ValueGeneratedNever();
        b.Property(w => w.Key).HasMaxLength(Word.MaxKeyLength).IsRequired();
        b.Property(w => w.Hint).HasMaxLength(Word.MaxHintLength);
        b.Property(w => w.Answers)
            .HasConversion(new ValueConverter<List<string>, string>(
                v => AnswersJson.Serialize(v),
                v => AnswersJson.Deserialize(v)))
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (a, c) => (a == null && c == null) || (a != null && c != null && a.SequenceEqual(c)),
                v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
                v => v.ToList()));
        b.Property(w => w.Answers).IsRequired();
    }
}

internal static class AnswersJson
{
    public static string Serialize(List<string> answers) => JsonSerializer.Serialize(answers);

    public static List<string> Deserialize(string json) =>
        string.IsNullOrEmpty(json) ? [] : JsonSerializer.Deserialize<List<string>>(json) ?? [];
}

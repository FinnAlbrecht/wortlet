using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Wortlet.Domain.Common;
using Wortlet.Domain.Folders;
using Wortlet.Domain.Languages;
using Wortlet.Domain.Recognition;
using Wortlet.Infrastructure.Identity;

namespace Wortlet.Infrastructure.Persistence;

/// <summary>
/// Gemeinsames Modell für alle Datenbanken. Pro Datenbank gibt es eine abgeleitete Klasse
/// mit eigenen Migrationen (<see cref="SqliteAppDbContext"/>, <see cref="PostgresAppDbContext"/>).
/// </summary>
public abstract class AppDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>, IUnitOfWork
{
    protected AppDbContext(DbContextOptions options) : base(options) { }

    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<RecognitionUsage> RecognitionUsages => Set<RecognitionUsage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    async Task IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);
}

public sealed class SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : AppDbContext(options);

public sealed class PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : AppDbContext(options);

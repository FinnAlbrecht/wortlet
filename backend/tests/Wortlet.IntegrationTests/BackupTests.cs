using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Wortlet.Infrastructure.Storage;

namespace Wortlet.IntegrationTests;

public class BackupTests(WortletFactory factory) : IClassFixture<WortletFactory>
{
    private static long Scalar(string file, string sql)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Pooling = false, Mode = SqliteOpenMode.ReadOnly }.ToString());
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }

    [Fact]
    public async Task Backup_contains_users_and_words()
    {
        var email = $"backup-{Guid.NewGuid():N}@example.test";
        await factory.CreateUserClientAsync(email);

        var file = await factory.Services.GetRequiredService<SqliteBackupService>().CreateBackupAsync("test");

        Assert.True(File.Exists(file));
        Assert.False(File.Exists(file + ".tmp"));
        Assert.Equal(1, Scalar(file, $"SELECT COUNT(*) FROM AspNetUsers WHERE Email = '{email}'"));
        Assert.True(Scalar(file, "SELECT COUNT(*) FROM Words") >= 319);
    }

    [Fact]
    public async Task Old_backups_are_removed()
    {
        using var app = factory.WithWebHostBuilder(b => b.UseSetting("Backup:KeepCount", "2"));
        var backups = app.Services.GetRequiredService<SqliteBackupService>();
        foreach (var file in backups.ListBackups()) File.Delete(file);

        await backups.CreateBackupAsync("a");
        await backups.CreateBackupAsync("b");
        var newest = await backups.CreateBackupAsync("c");

        var left = backups.ListBackups();
        Assert.Equal(2, left.Count);
        Assert.Equal(newest, left[0]);
        Assert.DoesNotContain(left, f => f.EndsWith("-a.db"));
    }

    [Fact]
    public async Task Worker_creates_a_backup_on_start()
    {
        using var app = factory.WithWebHostBuilder(b => b
            .UseSetting("Backup:Enabled", "true")
            .UseSetting("Backup:Directory", "worker-backups"));
        app.CreateClient(); // startet die App samt Hintergrunddienst
        var backups = app.Services.GetRequiredService<SqliteBackupService>();

        for (var i = 0; i < 50 && backups.ListBackups().Count == 0; i++) await Task.Delay(200);

        Assert.NotEmpty(backups.ListBackups());
    }
}

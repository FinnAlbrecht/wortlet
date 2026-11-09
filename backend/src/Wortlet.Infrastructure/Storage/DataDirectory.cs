namespace Wortlet.Infrastructure.Storage;

/// <summary>Ordner für die SQLite-Datenbank und den erzeugten JWT-Schlüssel (in Docker ein Volume).</summary>
public sealed class DataDirectory
{
    public DataDirectory(string path)
    {
        Path = System.IO.Path.GetFullPath(path);
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }
}

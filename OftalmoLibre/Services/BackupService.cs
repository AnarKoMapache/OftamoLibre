using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Services;

public sealed class BackupService
{
    public BackupRecord CreateBackup(string? notes = null)
    {
        DbPaths.EnsureDirectories();
        var fileName = $"OftalmoLibre_{DateTime.Now:yyyyMMdd_HHmmss}.db";
        var fullPath = Path.Combine(DbPaths.BackupDirectory, fileName);
        File.Copy(DbPaths.DatabasePath, fullPath, true);

        var record = new BackupRecord
        {
            FileName = fileName,
            FullPath = fullPath,
            CreatedAt = DateTime.Now,
            Notes = notes
        };

        record.Id = (int)Database.ExecuteInsert(
            """
            INSERT INTO backup_records (file_name, full_path, created_at, notes)
            VALUES (@file_name, @full_path, @created_at, @notes);
            """,
            new Dictionary<string, object?>
            {
                ["@file_name"] = record.FileName,
                ["@full_path"] = record.FullPath,
                ["@created_at"] = record.CreatedAt.ToString("s"),
                ["@notes"] = record.Notes
            });

        return record;
    }

    public List<BackupRecord> GetBackups()
    {
        return Database.Query(
            "SELECT id, file_name, full_path, created_at, notes FROM backup_records ORDER BY created_at DESC;",
            reader => new BackupRecord
            {
                Id = reader.GetInt32(0),
                FileName = reader.GetString(1),
                FullPath = reader.GetString(2),
                CreatedAt = DateTime.Parse(reader.GetString(3)),
                Notes = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
    }
}

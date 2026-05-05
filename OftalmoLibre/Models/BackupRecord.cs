namespace OftalmoLibre.Models;

public sealed class BackupRecord
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? Notes { get; set; }
}

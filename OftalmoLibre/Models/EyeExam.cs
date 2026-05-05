namespace OftalmoLibre.Models;

public sealed class EyeExam
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ProfessionalId { get; set; }
    public int? AttentionId { get; set; }
    public DateTime ExamDate { get; set; } = DateTime.Now;
    public string ExamType { get; set; } = string.Empty;
    public string? ResultSummary { get; set; }
    public string? Notes { get; set; }
}

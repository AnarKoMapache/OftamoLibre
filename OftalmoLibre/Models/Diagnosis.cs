namespace OftalmoLibre.Models;

public sealed class Diagnosis
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ProfessionalId { get; set; }
    public int? AttentionId { get; set; }
    public DateTime DiagnosisDate { get; set; } = DateTime.Now;
    public string? Code { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

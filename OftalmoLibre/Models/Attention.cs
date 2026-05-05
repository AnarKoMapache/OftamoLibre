namespace OftalmoLibre.Models;

public sealed class Attention
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ProfessionalId { get; set; }
    public int? AppointmentId { get; set; }
    public DateTime VisitDate { get; set; } = DateTime.Now;
    public string? ChiefComplaint { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? Plan { get; set; }
    public string? VisualAcuityRight { get; set; }
    public string? VisualAcuityLeft { get; set; }
}

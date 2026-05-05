namespace OftalmoLibre.Models;

public sealed class OpticalPrescription
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ProfessionalId { get; set; }
    public int? AttentionId { get; set; }
    public DateTime PrescriptionDate { get; set; } = DateTime.Now;
    public string? SphereRight { get; set; }
    public string? CylinderRight { get; set; }
    public string? AxisRight { get; set; }
    public string? SphereLeft { get; set; }
    public string? CylinderLeft { get; set; }
    public string? AxisLeft { get; set; }
    public string? AddPower { get; set; }
    public string? PupillaryDistance { get; set; }
    public string? Notes { get; set; }
}

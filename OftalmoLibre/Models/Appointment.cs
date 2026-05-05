namespace OftalmoLibre.Models;

public sealed class Appointment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int ProfessionalId { get; set; }
    public int ServiceId { get; set; }
    public DateTime ScheduledAt { get; set; } = DateTime.Now;
    public DateTime? EndAt { get; set; }
    public string Status { get; set; } = "Pendiente";
    public string PaymentStatus { get; set; } = "No Pagado";
    public string? Agenda { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

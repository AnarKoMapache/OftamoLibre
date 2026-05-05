namespace OftalmoLibre.Models;

public sealed class Payment
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public int? AttentionId { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Efectivo";
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

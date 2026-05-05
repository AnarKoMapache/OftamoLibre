namespace OftalmoLibre.Models;

public sealed class AppointmentListItem
{
    public int Id { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public DateTime? EndAt { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ProfessionalName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string? Agenda { get; set; }
    public string? Notes { get; set; }
    public string Display => $"{ScheduledAt:g} - {PatientName} / {ServiceName}";
}

public sealed class AttentionListItem
{
    public int Id { get; set; }
    public DateTime VisitDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ProfessionalName { get; set; } = string.Empty;
    public string? ChiefComplaint { get; set; }
}

public sealed class PrescriptionListItem
{
    public int Id { get; set; }
    public DateTime PrescriptionDate { get; set; }
    public string? DocumentNumber { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ProfessionalName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class ExamListItem
{
    public int Id { get; set; }
    public DateTime ExamDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ProfessionalName { get; set; } = string.Empty;
    public string ExamType { get; set; } = string.Empty;
}

public sealed class DiagnosisListItem
{
    public int Id { get; set; }
    public DateTime DiagnosisDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string ProfessionalName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class PaymentListItem
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Method { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

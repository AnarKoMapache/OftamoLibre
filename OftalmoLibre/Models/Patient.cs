using OftalmoLibre.Helpers;

namespace OftalmoLibre.Models;

public sealed class Patient
{
    public int Id { get; set; }
    public string RecordNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public DateTime? BirthDate { get; set; }
    public int Age => DateHelper.CalculateAge(BirthDate);
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Insurance { get; set; }
    public string? Occupation { get; set; }
    public string? MedicalHistory { get; set; }
    public string? OphthalmicHistory { get; set; }
    public bool UsesGlasses { get; set; }
    public bool ContactLenses { get; set; }
    public string? Allergies { get; set; }
    public string? CurrentMedications { get; set; }
    public string? GeneralNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

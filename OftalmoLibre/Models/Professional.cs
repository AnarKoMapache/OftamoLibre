namespace OftalmoLibre.Models;

public sealed class Professional
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string ProfessionalType { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string? RegistrationNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

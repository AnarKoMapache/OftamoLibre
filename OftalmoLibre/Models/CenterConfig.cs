namespace OftalmoLibre.Models;

public sealed class CenterConfig
{
    public int Id { get; set; } = 1;
    public string CenterName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string DefaultCurrency { get; set; } = "CLP";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}

namespace OftalmoLibre.Models;

public sealed class OphthalmologyService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
}

namespace OftalmoLibre.Models;

public sealed class LookupItem
{
    public int? Id { get; set; }
    public string Text { get; set; } = string.Empty;

    public override string ToString()
    {
        return Text;
    }
}

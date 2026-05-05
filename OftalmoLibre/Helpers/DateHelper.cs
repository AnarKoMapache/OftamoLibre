namespace OftalmoLibre.Helpers;

public static class DateHelper
{
    public static int CalculateAge(DateTime? birthDate)
    {
        if (birthDate is null)
        {
            return 0;
        }

        var today = DateTime.Today;
        var age = today.Year - birthDate.Value.Year;
        if (birthDate.Value.Date > today.AddYears(-age))
        {
            age--;
        }

        return Math.Max(age, 0);
    }
}

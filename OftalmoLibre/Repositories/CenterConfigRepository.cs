using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class CenterConfigRepository
{
    public CenterConfig Get()
    {
        return Database.QuerySingle(
                   """
                   SELECT id, center_name, address, phone, email, default_currency, updated_at
                   FROM center_config
                   WHERE id = 1
                   LIMIT 1;
                   """,
                   reader => new CenterConfig
                   {
                       Id = reader.GetInt32(0),
                       CenterName = reader.GetString(1),
                       Address = reader.IsDBNull(2) ? null : reader.GetString(2),
                       Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                       Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                       DefaultCurrency = reader.GetString(5),
                       UpdatedAt = DateTime.Parse(reader.GetString(6))
                   })
               ?? new CenterConfig { CenterName = Helpers.AppIdentity.DefaultCenterName };
    }

    public void Save(CenterConfig config)
    {
        Database.Execute(
            """
            UPDATE center_config
            SET center_name = @center_name,
                address = @address,
                phone = @phone,
                email = @email,
                default_currency = @default_currency,
                updated_at = @updated_at
            WHERE id = 1;
            """,
            new Dictionary<string, object?>
            {
                ["@center_name"] = config.CenterName,
                ["@address"] = config.Address,
                ["@phone"] = config.Phone,
                ["@email"] = config.Email,
                ["@default_currency"] = config.DefaultCurrency,
                ["@updated_at"] = DateTime.Now.ToString("s")
            });
    }
}

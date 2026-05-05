using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class PaymentRepository
{
    public List<PaymentListItem> GetAll()
    {
        return Database.Query(
            """
            SELECT p.id, p.payment_date, pa.full_name, p.amount, p.method, p.reference
            FROM payments p
            INNER JOIN patients pa ON pa.id = p.patient_id
            ORDER BY p.payment_date DESC;
            """,
            MapListItem);
    }

    public List<PaymentListItem> GetByPatient(int patientId)
    {
        return Database.Query(
            """
            SELECT p.id, p.payment_date, pa.full_name, p.amount, p.method, p.reference
            FROM payments p
            INNER JOIN patients pa ON pa.id = p.patient_id
            WHERE p.patient_id = @patient_id
            ORDER BY p.payment_date DESC;
            """,
            MapListItem,
            new Dictionary<string, object?> { ["@patient_id"] = patientId });
    }

    public List<PaymentListItem> GetDetailed(DateTime from, DateTime to)
    {
        return Database.Query(
            """
            SELECT p.id, p.payment_date, pa.full_name, p.amount, p.method, p.reference
            FROM payments p
            INNER JOIN patients pa ON pa.id = p.patient_id
            WHERE p.payment_date >= @from
              AND p.payment_date < @to
            ORDER BY p.payment_date DESC;
            """,
            MapListItem,
            new Dictionary<string, object?>
            {
                ["@from"] = from.Date.ToString("s"),
                ["@to"] = to.Date.AddDays(1).ToString("s")
            });
    }

    public decimal GetTotalByDay(DateTime day)
    {
        var value = Database.Scalar(
            """
            SELECT IFNULL(SUM(amount), 0)
            FROM payments
            WHERE payment_date >= @start
              AND payment_date < @end;
            """,
            new Dictionary<string, object?>
            {
                ["@start"] = day.Date.ToString("s"),
                ["@end"] = day.Date.AddDays(1).ToString("s")
            });

        return Convert.ToDecimal(value ?? 0m);
    }

    public Payment? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, patient_id, appointment_id, attention_id, payment_date, amount, method, reference, notes
            FROM payments
            WHERE id = @id
            LIMIT 1;
            """,
            reader => new Payment
            {
                Id = reader.GetInt32(0),
                PatientId = reader.GetInt32(1),
                AppointmentId = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                AttentionId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
                PaymentDate = DateTime.Parse(reader.GetString(4)),
                Amount = reader.GetDecimal(5),
                Method = reader.GetString(6),
                Reference = reader.IsDBNull(7) ? null : reader.GetString(7),
                Notes = reader.IsDBNull(8) ? null : reader.GetString(8)
            },
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public void Save(Payment payment)
    {
        if (payment.Id == 0)
        {
            payment.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO payments (patient_id, appointment_id, attention_id, payment_date, amount, method, reference, notes)
                VALUES (@patient_id, @appointment_id, @attention_id, @payment_date, @amount, @method, @reference, @notes);
                """,
                ToParameters(payment, false));
            return;
        }

        Database.Execute(
            """
            UPDATE payments
            SET patient_id = @patient_id,
                appointment_id = @appointment_id,
                attention_id = @attention_id,
                payment_date = @payment_date,
                amount = @amount,
                method = @method,
                reference = @reference,
                notes = @notes
            WHERE id = @id;
            """,
            ToParameters(payment, true));
    }

    private static PaymentListItem MapListItem(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new PaymentListItem
        {
            Id = reader.GetInt32(0),
            PaymentDate = DateTime.Parse(reader.GetString(1)),
            PatientName = reader.GetString(2),
            Amount = reader.GetDecimal(3),
            Method = reader.GetString(4),
            Reference = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    private static Dictionary<string, object?> ToParameters(Payment payment, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@patient_id"] = payment.PatientId,
            ["@appointment_id"] = payment.AppointmentId,
            ["@attention_id"] = payment.AttentionId,
            ["@payment_date"] = payment.PaymentDate.ToString("s"),
            ["@amount"] = payment.Amount,
            ["@method"] = payment.Method,
            ["@reference"] = payment.Reference,
            ["@notes"] = payment.Notes
        };

        if (includeId)
        {
            values["@id"] = payment.Id;
        }

        return values;
    }
}

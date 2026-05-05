using Microsoft.Data.Sqlite;
using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class AppointmentRepository
{
    public List<AppointmentListItem> GetAll(DateTime? day = null, string? status = null)
    {
        return Database.Query(
            """
            SELECT a.id,
                   p.record_number,
                   a.scheduled_at,
                   a.end_at,
                   p.full_name,
                   pr.full_name,
                   s.name,
                   a.status,
                   a.payment_status,
                   a.agenda,
                   a.notes
            FROM appointments a
            INNER JOIN patients p ON p.id = a.patient_id
            INNER JOIN professionals pr ON pr.id = a.professional_id
            INNER JOIN services s ON s.id = a.service_id
            WHERE (@start IS NULL OR a.scheduled_at >= @start)
              AND (@end IS NULL OR a.scheduled_at < @end)
              AND (@status = '' OR a.status = @status)
            ORDER BY a.scheduled_at;
            """,
            MapListItem,
            new Dictionary<string, object?>
            {
                ["@start"] = day?.Date.ToString("s"),
                ["@end"] = day?.Date.AddDays(1).ToString("s"),
                ["@status"] = status?.Trim() ?? string.Empty
            });
    }

    public List<AppointmentListItem> GetByPatient(int patientId)
    {
        return Database.Query(
            """
            SELECT a.id,
                   p.record_number,
                   a.scheduled_at,
                   a.end_at,
                   p.full_name,
                   pr.full_name,
                   s.name,
                   a.status,
                   a.payment_status,
                   a.agenda,
                   a.notes
            FROM appointments a
            INNER JOIN patients p ON p.id = a.patient_id
            INNER JOIN professionals pr ON pr.id = a.professional_id
            INNER JOIN services s ON s.id = a.service_id
            WHERE a.patient_id = @patient_id
            ORDER BY a.scheduled_at DESC;
            """,
            MapListItem,
            new Dictionary<string, object?> { ["@patient_id"] = patientId });
    }

    public List<AppointmentListItem> GetUpcoming(int limit)
    {
        return Database.Query(
            $"""
            SELECT a.id,
                   p.record_number,
                   a.scheduled_at,
                   a.end_at,
                   p.full_name,
                   pr.full_name,
                   s.name,
                   a.status,
                   a.payment_status,
                   a.agenda,
                   a.notes
            FROM appointments a
            INNER JOIN patients p ON p.id = a.patient_id
            INNER JOIN professionals pr ON pr.id = a.professional_id
            INNER JOIN services s ON s.id = a.service_id
            WHERE a.scheduled_at >= @now
            ORDER BY a.scheduled_at
            LIMIT {limit};
            """,
            MapListItem,
            new Dictionary<string, object?> { ["@now"] = DateTime.Now.ToString("s") });
    }

    public Dictionary<string, int> GetStatusCountsForDay(DateTime day)
    {
        var items = Database.Query(
            """
            SELECT status, COUNT(*)
            FROM appointments
            WHERE scheduled_at >= @start
              AND scheduled_at < @end
            GROUP BY status;
            """,
            reader => new KeyValuePair<string, int>(reader.GetString(0), reader.GetInt32(1)),
            new Dictionary<string, object?>
            {
                ["@start"] = day.Date.ToString("s"),
                ["@end"] = day.Date.AddDays(1).ToString("s")
            });

        var values = items.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
        values["Total"] = Convert.ToInt32(Database.Scalar(
            """
            SELECT COUNT(*)
            FROM appointments
            WHERE scheduled_at >= @start
              AND scheduled_at < @end;
            """,
            new Dictionary<string, object?>
            {
                ["@start"] = day.Date.ToString("s"),
                ["@end"] = day.Date.AddDays(1).ToString("s")
            }) ?? 0);

        return values;
    }

    public List<AppointmentListItem> GetDetailed(DateTime from, DateTime to)
    {
        return Database.Query(
            """
            SELECT a.id,
                   p.record_number,
                   a.scheduled_at,
                   a.end_at,
                   p.full_name,
                   pr.full_name,
                   s.name,
                   a.status,
                   a.payment_status,
                   a.agenda,
                   a.notes
            FROM appointments a
            INNER JOIN patients p ON p.id = a.patient_id
            INNER JOIN professionals pr ON pr.id = a.professional_id
            INNER JOIN services s ON s.id = a.service_id
            WHERE a.scheduled_at >= @from
              AND a.scheduled_at < @to
            ORDER BY a.scheduled_at;
            """,
            MapListItem,
            new Dictionary<string, object?>
            {
                ["@from"] = from.Date.ToString("s"),
                ["@to"] = to.Date.AddDays(1).ToString("s")
            });
    }

    public Appointment? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, patient_id, professional_id, service_id, scheduled_at, end_at, status, payment_status, agenda, notes, created_at
            FROM appointments
            WHERE id = @id
            LIMIT 1;
            """,
            reader => new Appointment
            {
                Id = reader.GetInt32(0),
                PatientId = reader.GetInt32(1),
                ProfessionalId = reader.GetInt32(2),
                ServiceId = reader.GetInt32(3),
                ScheduledAt = DateTime.Parse(reader.GetString(4)),
                EndAt = reader.IsDBNull(5) ? null : DateTime.Parse(reader.GetString(5)),
                Status = reader.GetString(6),
                PaymentStatus = reader.IsDBNull(7) ? "No Pagado" : reader.GetString(7),
                Agenda = reader.IsDBNull(8) ? null : reader.GetString(8),
                Notes = reader.IsDBNull(9) ? null : reader.GetString(9),
                CreatedAt = DateTime.Parse(reader.GetString(10))
            },
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public void Save(Appointment appointment)
    {
        if (appointment.Id == 0)
        {
            appointment.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO appointments (patient_id, professional_id, service_id, scheduled_at, end_at, status, payment_status, agenda, notes, created_at)
                VALUES (@patient_id, @professional_id, @service_id, @scheduled_at, @end_at, @status, @payment_status, @agenda, @notes, @created_at);
                """,
                ToParameters(appointment, false));
            return;
        }

        Database.Execute(
            """
            UPDATE appointments
            SET patient_id = @patient_id,
                professional_id = @professional_id,
                service_id = @service_id,
                scheduled_at = @scheduled_at,
                end_at = @end_at,
                status = @status,
                payment_status = @payment_status,
                agenda = @agenda,
                notes = @notes
            WHERE id = @id;
            """,
            ToParameters(appointment, true));
    }

    private static Dictionary<string, object?> ToParameters(Appointment appointment, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@patient_id"] = appointment.PatientId,
            ["@professional_id"] = appointment.ProfessionalId,
            ["@service_id"] = appointment.ServiceId,
            ["@scheduled_at"] = appointment.ScheduledAt.ToString("s"),
            ["@end_at"] = (appointment.EndAt ?? appointment.ScheduledAt.AddMinutes(15)).ToString("s"),
            ["@status"] = appointment.Status,
            ["@payment_status"] = string.IsNullOrWhiteSpace(appointment.PaymentStatus) ? "No Pagado" : appointment.PaymentStatus,
            ["@agenda"] = string.IsNullOrWhiteSpace(appointment.Agenda) ? "BOX CONDELL" : appointment.Agenda,
            ["@notes"] = appointment.Notes,
            ["@created_at"] = appointment.CreatedAt.ToString("s")
        };

        if (includeId)
        {
            values["@id"] = appointment.Id;
        }

        return values;
    }

    private static AppointmentListItem MapListItem(SqliteDataReader reader)
    {
        return new AppointmentListItem
        {
            Id = reader.GetInt32(0),
            RecordNumber = reader.GetString(1),
            ScheduledAt = DateTime.Parse(reader.GetString(2)),
            EndAt = reader.IsDBNull(3) ? null : DateTime.Parse(reader.GetString(3)),
            PatientName = reader.GetString(4),
            ProfessionalName = reader.GetString(5),
            ServiceName = reader.GetString(6),
            Status = reader.GetString(7),
            PaymentStatus = reader.IsDBNull(8) ? "No Pagado" : reader.GetString(8),
            Agenda = reader.IsDBNull(9) ? null : reader.GetString(9),
            Notes = reader.IsDBNull(10) ? null : reader.GetString(10)
        };
    }
}

using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class AttentionRepository
{
    public List<AttentionListItem> GetAll(string? search = null)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var sql = """
            SELECT a.id, a.visit_date, p.full_name, pr.full_name, a.chief_complaint
            FROM attentions a
            INNER JOIN patients p ON p.id = a.patient_id
            INNER JOIN professionals pr ON pr.id = a.professional_id
            """ + (hasSearch ? " WHERE p.full_name LIKE @search OR a.chief_complaint LIKE @search" : "") + """

            ORDER BY a.visit_date DESC;
            """;

        var parameters = hasSearch
            ? new Dictionary<string, object?> { ["@search"] = $"%{search}%" }
            : null;

        return Database.Query(
            sql,
            reader => new AttentionListItem
            {
                Id = reader.GetInt32(0),
                VisitDate = DateTime.Parse(reader.GetString(1)),
                PatientName = reader.GetString(2),
                ProfessionalName = reader.GetString(3),
                ChiefComplaint = reader.IsDBNull(4) ? null : reader.GetString(4)
            },
            parameters);
    }

    public List<AttentionListItem> GetByPatient(int patientId)
    {
        return Database.Query(
            """
            SELECT a.id, a.visit_date, p.full_name, pr.full_name, a.chief_complaint
            FROM attentions a
            INNER JOIN patients p ON p.id = a.patient_id
            INNER JOIN professionals pr ON pr.id = a.professional_id
            WHERE a.patient_id = @patient_id
            ORDER BY a.visit_date DESC;
            """,
            reader => new AttentionListItem
            {
                Id = reader.GetInt32(0),
                VisitDate = DateTime.Parse(reader.GetString(1)),
                PatientName = reader.GetString(2),
                ProfessionalName = reader.GetString(3),
                ChiefComplaint = reader.IsDBNull(4) ? null : reader.GetString(4)
            },
            new Dictionary<string, object?> { ["@patient_id"] = patientId });
    }

    public List<Attention> GetByPatientForLookup(int patientId)
    {
        return Database.Query(
            """
            SELECT id, patient_id, professional_id, appointment_id, visit_date, chief_complaint, clinical_notes, plan, visual_acuity_right, visual_acuity_left
            FROM attentions
            WHERE patient_id = @patient_id
            ORDER BY visit_date DESC;
            """,
            Map,
            new Dictionary<string, object?> { ["@patient_id"] = patientId });
    }

    public Attention? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, patient_id, professional_id, appointment_id, visit_date, chief_complaint, clinical_notes, plan, visual_acuity_right, visual_acuity_left
            FROM attentions
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public void Save(Attention attention)
    {
        if (attention.Id == 0)
        {
            attention.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO attentions (patient_id, professional_id, appointment_id, visit_date, chief_complaint, clinical_notes, plan, visual_acuity_right, visual_acuity_left)
                VALUES (@patient_id, @professional_id, @appointment_id, @visit_date, @chief_complaint, @clinical_notes, @plan, @visual_acuity_right, @visual_acuity_left);
                """,
                ToParameters(attention, false));
            return;
        }

        Database.Execute(
            """
            UPDATE attentions
            SET patient_id = @patient_id,
                professional_id = @professional_id,
                appointment_id = @appointment_id,
                visit_date = @visit_date,
                chief_complaint = @chief_complaint,
                clinical_notes = @clinical_notes,
                plan = @plan,
                visual_acuity_right = @visual_acuity_right,
                visual_acuity_left = @visual_acuity_left
            WHERE id = @id;
            """,
            ToParameters(attention, true));
    }

    private static Dictionary<string, object?> ToParameters(Attention attention, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@patient_id"] = attention.PatientId,
            ["@professional_id"] = attention.ProfessionalId,
            ["@appointment_id"] = attention.AppointmentId,
            ["@visit_date"] = attention.VisitDate.ToString("s"),
            ["@chief_complaint"] = attention.ChiefComplaint,
            ["@clinical_notes"] = attention.ClinicalNotes,
            ["@plan"] = attention.Plan,
            ["@visual_acuity_right"] = attention.VisualAcuityRight,
            ["@visual_acuity_left"] = attention.VisualAcuityLeft
        };

        if (includeId)
        {
            values["@id"] = attention.Id;
        }

        return values;
    }

    private static Attention Map(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new Attention
        {
            Id = reader.GetInt32(0),
            PatientId = reader.GetInt32(1),
            ProfessionalId = reader.GetInt32(2),
            AppointmentId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            VisitDate = DateTime.Parse(reader.GetString(4)),
            ChiefComplaint = reader.IsDBNull(5) ? null : reader.GetString(5),
            ClinicalNotes = reader.IsDBNull(6) ? null : reader.GetString(6),
            Plan = reader.IsDBNull(7) ? null : reader.GetString(7),
            VisualAcuityRight = reader.IsDBNull(8) ? null : reader.GetString(8),
            VisualAcuityLeft = reader.IsDBNull(9) ? null : reader.GetString(9)
        };
    }
}

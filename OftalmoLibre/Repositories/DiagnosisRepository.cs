using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class DiagnosisRepository
{
    public List<DiagnosisListItem> GetAll()
    {
        return Database.Query(
            """
            SELECT d.id, d.diagnosis_date, p.full_name, pr.full_name, d.description
            FROM diagnoses d
            INNER JOIN patients p ON p.id = d.patient_id
            INNER JOIN professionals pr ON pr.id = d.professional_id
            ORDER BY d.diagnosis_date DESC;
            """,
            MapListItem);
    }

    public List<DiagnosisListItem> GetByPatient(int patientId)
    {
        return Database.Query(
            """
            SELECT d.id, d.diagnosis_date, p.full_name, pr.full_name, d.description
            FROM diagnoses d
            INNER JOIN patients p ON p.id = d.patient_id
            INNER JOIN professionals pr ON pr.id = d.professional_id
            WHERE d.patient_id = @patient_id
            ORDER BY d.diagnosis_date DESC;
            """,
            MapListItem,
            new Dictionary<string, object?> { ["@patient_id"] = patientId });
    }

    public Diagnosis? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, patient_id, professional_id, attention_id, diagnosis_date, code, description, notes
            FROM diagnoses
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public void Save(Diagnosis diagnosis)
    {
        if (diagnosis.Id == 0)
        {
            diagnosis.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO diagnoses (patient_id, professional_id, attention_id, diagnosis_date, code, description, notes)
                VALUES (@patient_id, @professional_id, @attention_id, @diagnosis_date, @code, @description, @notes);
                """,
                ToParameters(diagnosis, false));
            return;
        }

        Database.Execute(
            """
            UPDATE diagnoses
            SET patient_id = @patient_id,
                professional_id = @professional_id,
                attention_id = @attention_id,
                diagnosis_date = @diagnosis_date,
                code = @code,
                description = @description,
                notes = @notes
            WHERE id = @id;
            """,
            ToParameters(diagnosis, true));
    }

    private static DiagnosisListItem MapListItem(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new DiagnosisListItem
        {
            Id = reader.GetInt32(0),
            DiagnosisDate = DateTime.Parse(reader.GetString(1)),
            PatientName = reader.GetString(2),
            ProfessionalName = reader.GetString(3),
            Description = reader.GetString(4)
        };
    }

    private static Diagnosis Map(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new Diagnosis
        {
            Id = reader.GetInt32(0),
            PatientId = reader.GetInt32(1),
            ProfessionalId = reader.GetInt32(2),
            AttentionId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            DiagnosisDate = DateTime.Parse(reader.GetString(4)),
            Code = reader.IsDBNull(5) ? null : reader.GetString(5),
            Description = reader.GetString(6),
            Notes = reader.IsDBNull(7) ? null : reader.GetString(7)
        };
    }

    private static Dictionary<string, object?> ToParameters(Diagnosis diagnosis, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@patient_id"] = diagnosis.PatientId,
            ["@professional_id"] = diagnosis.ProfessionalId,
            ["@attention_id"] = diagnosis.AttentionId,
            ["@diagnosis_date"] = diagnosis.DiagnosisDate.ToString("s"),
            ["@code"] = diagnosis.Code,
            ["@description"] = diagnosis.Description,
            ["@notes"] = diagnosis.Notes
        };

        if (includeId)
        {
            values["@id"] = diagnosis.Id;
        }

        return values;
    }
}

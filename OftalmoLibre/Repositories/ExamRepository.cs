using OftalmoLibre.Data;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class ExamRepository
{
    public List<ExamListItem> GetAll()
    {
        return Database.Query(
            """
            SELECT e.id, e.exam_date, p.full_name, pr.full_name, e.exam_type
            FROM eye_exams e
            INNER JOIN patients p ON p.id = e.patient_id
            INNER JOIN professionals pr ON pr.id = e.professional_id
            ORDER BY e.exam_date DESC;
            """,
            MapListItem);
    }

    public List<ExamListItem> GetByPatient(int patientId)
    {
        return Database.Query(
            """
            SELECT e.id, e.exam_date, p.full_name, pr.full_name, e.exam_type
            FROM eye_exams e
            INNER JOIN patients p ON p.id = e.patient_id
            INNER JOIN professionals pr ON pr.id = e.professional_id
            WHERE e.patient_id = @patient_id
            ORDER BY e.exam_date DESC;
            """,
            MapListItem,
            new Dictionary<string, object?> { ["@patient_id"] = patientId });
    }

    public EyeExam? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, patient_id, professional_id, attention_id, exam_date, exam_type, result_summary, notes
            FROM eye_exams
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public void Save(EyeExam exam)
    {
        if (exam.Id == 0)
        {
            exam.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO eye_exams (patient_id, professional_id, attention_id, exam_date, exam_type, result_summary, notes)
                VALUES (@patient_id, @professional_id, @attention_id, @exam_date, @exam_type, @result_summary, @notes);
                """,
                ToParameters(exam, false));
            return;
        }

        Database.Execute(
            """
            UPDATE eye_exams
            SET patient_id = @patient_id,
                professional_id = @professional_id,
                attention_id = @attention_id,
                exam_date = @exam_date,
                exam_type = @exam_type,
                result_summary = @result_summary,
                notes = @notes
            WHERE id = @id;
            """,
            ToParameters(exam, true));
    }

    private static ExamListItem MapListItem(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new ExamListItem
        {
            Id = reader.GetInt32(0),
            ExamDate = DateTime.Parse(reader.GetString(1)),
            PatientName = reader.GetString(2),
            ProfessionalName = reader.GetString(3),
            ExamType = reader.GetString(4)
        };
    }

    private static EyeExam Map(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new EyeExam
        {
            Id = reader.GetInt32(0),
            PatientId = reader.GetInt32(1),
            ProfessionalId = reader.GetInt32(2),
            AttentionId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            ExamDate = DateTime.Parse(reader.GetString(4)),
            ExamType = reader.GetString(5),
            ResultSummary = reader.IsDBNull(6) ? null : reader.GetString(6),
            Notes = reader.IsDBNull(7) ? null : reader.GetString(7)
        };
    }

    private static Dictionary<string, object?> ToParameters(EyeExam exam, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@patient_id"] = exam.PatientId,
            ["@professional_id"] = exam.ProfessionalId,
            ["@attention_id"] = exam.AttentionId,
            ["@exam_date"] = exam.ExamDate.ToString("s"),
            ["@exam_type"] = exam.ExamType,
            ["@result_summary"] = exam.ResultSummary,
            ["@notes"] = exam.Notes
        };

        if (includeId)
        {
            values["@id"] = exam.Id;
        }

        return values;
    }
}

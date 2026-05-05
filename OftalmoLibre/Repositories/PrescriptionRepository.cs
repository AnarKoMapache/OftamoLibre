using OftalmoLibre.Data;
using OftalmoLibre.Helpers;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class PrescriptionRepository
{
    public List<PrescriptionListItem> GetAll(string? search = null)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(search);
        var normalizedSearch = DocumentNumberHelper.NormalizeForComparison(search);
        var sql = """
            SELECT p.id, p.prescription_date, pa.document_number, pa.full_name, pr.full_name, p.notes
            FROM prescriptions p
            INNER JOIN patients pa ON pa.id = p.patient_id
            INNER JOIN professionals pr ON pr.id = p.professional_id
            """ + (hasSearch ? " WHERE pa.full_name LIKE @search OR IFNULL(pa.document_number, '') LIKE @search OR REPLACE(REPLACE(UPPER(IFNULL(pa.document_number, '')), '.', ''), '-', '') LIKE @normalized_search OR IFNULL(p.notes, '') LIKE @search" : "") + """

            ORDER BY p.prescription_date DESC;
            """;

        var parameters = hasSearch
            ? new Dictionary<string, object?>
            {
                ["@search"] = $"%{search}%",
                ["@normalized_search"] = $"%{normalizedSearch}%"
            }
            : null;

        return Database.Query(sql, MapListItem, parameters);
    }

    public List<PrescriptionListItem> GetByPatient(int patientId)
    {
        return Database.Query(
            """
            SELECT p.id, p.prescription_date, pa.document_number, pa.full_name, pr.full_name, p.notes
            FROM prescriptions p
            INNER JOIN patients pa ON pa.id = p.patient_id
            INNER JOIN professionals pr ON pr.id = p.professional_id
            WHERE p.patient_id = @patient_id
            ORDER BY p.prescription_date DESC;
            """,
            MapListItem,
            new Dictionary<string, object?> { ["@patient_id"] = patientId });
    }

    public OpticalPrescription? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, patient_id, professional_id, attention_id, prescription_date, sphere_right, cylinder_right, axis_right,
                   sphere_left, cylinder_left, axis_left, add_power, pupillary_distance, notes
            FROM prescriptions
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public void Save(OpticalPrescription prescription)
    {
        if (prescription.Id == 0)
        {
            prescription.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO prescriptions (patient_id, professional_id, attention_id, prescription_date, sphere_right, cylinder_right, axis_right,
                                           sphere_left, cylinder_left, axis_left, add_power, pupillary_distance, notes)
                VALUES (@patient_id, @professional_id, @attention_id, @prescription_date, @sphere_right, @cylinder_right, @axis_right,
                        @sphere_left, @cylinder_left, @axis_left, @add_power, @pupillary_distance, @notes);
                """,
                ToParameters(prescription, false));
            return;
        }

        Database.Execute(
            """
            UPDATE prescriptions
            SET patient_id = @patient_id,
                professional_id = @professional_id,
                attention_id = @attention_id,
                prescription_date = @prescription_date,
                sphere_right = @sphere_right,
                cylinder_right = @cylinder_right,
                axis_right = @axis_right,
                sphere_left = @sphere_left,
                cylinder_left = @cylinder_left,
                axis_left = @axis_left,
                add_power = @add_power,
                pupillary_distance = @pupillary_distance,
                notes = @notes
            WHERE id = @id;
            """,
            ToParameters(prescription, true));
    }

    private static PrescriptionListItem MapListItem(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new PrescriptionListItem
        {
            Id = reader.GetInt32(0),
            PrescriptionDate = DateTime.Parse(reader.GetString(1)),
            DocumentNumber = reader.IsDBNull(2) ? null : reader.GetString(2),
            PatientName = reader.GetString(3),
            ProfessionalName = reader.GetString(4),
            Notes = reader.IsDBNull(5) ? null : reader.GetString(5)
        };
    }

    private static OpticalPrescription Map(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new OpticalPrescription
        {
            Id = reader.GetInt32(0),
            PatientId = reader.GetInt32(1),
            ProfessionalId = reader.GetInt32(2),
            AttentionId = reader.IsDBNull(3) ? null : reader.GetInt32(3),
            PrescriptionDate = DateTime.Parse(reader.GetString(4)),
            SphereRight = reader.IsDBNull(5) ? null : reader.GetString(5),
            CylinderRight = reader.IsDBNull(6) ? null : reader.GetString(6),
            AxisRight = reader.IsDBNull(7) ? null : reader.GetString(7),
            SphereLeft = reader.IsDBNull(8) ? null : reader.GetString(8),
            CylinderLeft = reader.IsDBNull(9) ? null : reader.GetString(9),
            AxisLeft = reader.IsDBNull(10) ? null : reader.GetString(10),
            AddPower = reader.IsDBNull(11) ? null : reader.GetString(11),
            PupillaryDistance = reader.IsDBNull(12) ? null : reader.GetString(12),
            Notes = reader.IsDBNull(13) ? null : reader.GetString(13)
        };
    }

    private static Dictionary<string, object?> ToParameters(OpticalPrescription prescription, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@patient_id"] = prescription.PatientId,
            ["@professional_id"] = prescription.ProfessionalId,
            ["@attention_id"] = prescription.AttentionId,
            ["@prescription_date"] = prescription.PrescriptionDate.ToString("s"),
            ["@sphere_right"] = prescription.SphereRight,
            ["@cylinder_right"] = prescription.CylinderRight,
            ["@axis_right"] = prescription.AxisRight,
            ["@sphere_left"] = prescription.SphereLeft,
            ["@cylinder_left"] = prescription.CylinderLeft,
            ["@axis_left"] = prescription.AxisLeft,
            ["@add_power"] = prescription.AddPower,
            ["@pupillary_distance"] = prescription.PupillaryDistance,
            ["@notes"] = prescription.Notes
        };

        if (includeId)
        {
            values["@id"] = prescription.Id;
        }

        return values;
    }
}

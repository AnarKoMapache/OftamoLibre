using Microsoft.Data.Sqlite;
using OftalmoLibre.Data;
using OftalmoLibre.Helpers;
using OftalmoLibre.Models;

namespace OftalmoLibre.Repositories;

public sealed class PatientRepository
{
    public List<Patient> GetAll(string? search = null, bool includeInactive = true)
    {
        var term = search?.Trim() ?? string.Empty;
        var normalizedSearch = DocumentNumberHelper.NormalizeForComparison(term);
        return Database.Query(
            """
            SELECT id, record_number, full_name, document_number, birth_date, phone1, phone2, email, address, insurance, occupation,
                   medical_history, ophthalmic_history, uses_glasses, contact_lenses, allergies, current_medications, general_notes, is_active, created_at
            FROM patients
            WHERE (@search = ''
                    OR record_number LIKE @like
                    OR full_name LIKE @like
                    OR IFNULL(document_number, '') LIKE @like
                    OR REPLACE(REPLACE(UPPER(IFNULL(document_number, '')), '.', ''), '-', '') LIKE @normalized_like)
              AND (@include_inactive = 1 OR is_active = 1)
            ORDER BY full_name;
            """,
            Map,
            new Dictionary<string, object?>
            {
                ["@search"] = term,
                ["@like"] = $"%{term}%",
                ["@normalized_like"] = $"%{normalizedSearch}%",
                ["@include_inactive"] = includeInactive ? 1 : 0
            });
    }

    public List<Patient> GetActive()
    {
        return GetAll(includeInactive: false);
    }

    public Patient? GetById(int id)
    {
        return Database.QuerySingle(
            """
            SELECT id, record_number, full_name, document_number, birth_date, phone1, phone2, email, address, insurance, occupation,
                   medical_history, ophthalmic_history, uses_glasses, contact_lenses, allergies, current_medications, general_notes, is_active, created_at
            FROM patients
            WHERE id = @id
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@id"] = id });
    }

    public Patient? GetByRecordNumber(string recordNumber)
    {
        if (string.IsNullOrWhiteSpace(recordNumber))
        {
            return null;
        }

        return Database.QuerySingle(
            """
            SELECT id, record_number, full_name, document_number, birth_date, phone1, phone2, email, address, insurance, occupation,
                   medical_history, ophthalmic_history, uses_glasses, contact_lenses, allergies, current_medications, general_notes, is_active, created_at
            FROM patients
            WHERE lower(record_number) = lower(@record_number)
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@record_number"] = recordNumber.Trim() });
    }

    public Patient? GetByDocumentNumber(string documentNumber)
    {
        var normalizedDocument = DocumentNumberHelper.NormalizeForComparison(documentNumber);
        if (string.IsNullOrWhiteSpace(normalizedDocument))
        {
            return null;
        }

        return Database.QuerySingle(
            """
            SELECT id, record_number, full_name, document_number, birth_date, phone1, phone2, email, address, insurance, occupation,
                   medical_history, ophthalmic_history, uses_glasses, contact_lenses, allergies, current_medications, general_notes, is_active, created_at
            FROM patients
            WHERE REPLACE(REPLACE(UPPER(IFNULL(document_number, '')), '.', ''), '-', '') = @document_number
            LIMIT 1;
            """,
            Map,
            new Dictionary<string, object?> { ["@document_number"] = normalizedDocument });
    }

    public bool ExistsDocument(string? documentNumber, int? excludeId = null)
    {
        var normalizedDocument = DocumentNumberHelper.NormalizeForComparison(documentNumber);
        if (string.IsNullOrWhiteSpace(normalizedDocument))
        {
            return false;
        }

        return Convert.ToInt64(Database.Scalar(
            """
            SELECT COUNT(*)
            FROM patients
            WHERE REPLACE(REPLACE(UPPER(IFNULL(document_number, '')), '.', ''), '-', '') = @document_number
              AND (@exclude_id IS NULL OR id <> @exclude_id);
            """,
            new Dictionary<string, object?>
            {
                ["@document_number"] = normalizedDocument,
                ["@exclude_id"] = excludeId
            }) ?? 0L) > 0;
    }

    public void Save(Patient patient)
    {
        patient.RecordNumber = string.IsNullOrWhiteSpace(patient.RecordNumber) ? GenerateRecordNumber() : patient.RecordNumber.Trim();
        patient.DocumentNumber = DocumentNumberHelper.Normalize(patient.DocumentNumber);

        if (patient.Id == 0)
        {
            patient.Id = (int)Database.ExecuteInsert(
                """
                INSERT INTO patients (record_number, full_name, document_number, birth_date, phone1, phone2, email, address, insurance, occupation,
                                      medical_history, ophthalmic_history, uses_glasses, contact_lenses, allergies, current_medications, general_notes, is_active, created_at)
                VALUES (@record_number, @full_name, @document_number, @birth_date, @phone1, @phone2, @email, @address, @insurance, @occupation,
                        @medical_history, @ophthalmic_history, @uses_glasses, @contact_lenses, @allergies, @current_medications, @general_notes, @is_active, @created_at);
                """,
                ToParameters(patient, false));
            return;
        }

        Database.Execute(
            """
            UPDATE patients
            SET record_number = @record_number,
                full_name = @full_name,
                document_number = @document_number,
                birth_date = @birth_date,
                phone1 = @phone1,
                phone2 = @phone2,
                email = @email,
                address = @address,
                insurance = @insurance,
                occupation = @occupation,
                medical_history = @medical_history,
                ophthalmic_history = @ophthalmic_history,
                uses_glasses = @uses_glasses,
                contact_lenses = @contact_lenses,
                allergies = @allergies,
                current_medications = @current_medications,
                general_notes = @general_notes,
                is_active = @is_active
            WHERE id = @id;
            """,
            ToParameters(patient, true));
    }

    public void SetActive(int id, bool isActive)
    {
        Database.Execute(
            "UPDATE patients SET is_active = @is_active WHERE id = @id;",
            new Dictionary<string, object?>
            {
                ["@id"] = id,
                ["@is_active"] = isActive ? 1 : 0
            });
    }

    public string GenerateRecordNumber()
    {
        var maxId = Convert.ToInt64(Database.Scalar("SELECT IFNULL(MAX(id), 0) FROM patients;") ?? 0L);
        return $"CLI-{(maxId + 1):D6}";
    }

    private static Dictionary<string, object?> ToParameters(Patient patient, bool includeId)
    {
        var values = new Dictionary<string, object?>
        {
            ["@record_number"] = patient.RecordNumber.Trim(),
            ["@full_name"] = patient.FullName.Trim(),
            ["@document_number"] = DocumentNumberHelper.Normalize(patient.DocumentNumber),
            ["@birth_date"] = patient.BirthDate?.ToString("yyyy-MM-dd"),
            ["@phone1"] = patient.Phone1,
            ["@phone2"] = patient.Phone2,
            ["@email"] = patient.Email,
            ["@address"] = patient.Address,
            ["@insurance"] = patient.Insurance,
            ["@occupation"] = patient.Occupation,
            ["@medical_history"] = patient.MedicalHistory,
            ["@ophthalmic_history"] = patient.OphthalmicHistory,
            ["@uses_glasses"] = patient.UsesGlasses ? 1 : 0,
            ["@contact_lenses"] = patient.ContactLenses ? 1 : 0,
            ["@allergies"] = patient.Allergies,
            ["@current_medications"] = patient.CurrentMedications,
            ["@general_notes"] = patient.GeneralNotes,
            ["@is_active"] = patient.IsActive ? 1 : 0,
            ["@created_at"] = patient.CreatedAt.ToString("s")
        };

        if (includeId)
        {
            values["@id"] = patient.Id;
        }

        return values;
    }

    private static Patient Map(SqliteDataReader reader)
    {
        return new Patient
        {
            Id = reader.GetInt32(0),
            RecordNumber = reader.GetString(1),
            FullName = reader.GetString(2),
            DocumentNumber = reader.IsDBNull(3) ? null : reader.GetString(3),
            BirthDate = reader.IsDBNull(4) ? null : DateTime.Parse(reader.GetString(4)),
            Phone1 = reader.IsDBNull(5) ? null : reader.GetString(5),
            Phone2 = reader.IsDBNull(6) ? null : reader.GetString(6),
            Email = reader.IsDBNull(7) ? null : reader.GetString(7),
            Address = reader.IsDBNull(8) ? null : reader.GetString(8),
            Insurance = reader.IsDBNull(9) ? null : reader.GetString(9),
            Occupation = reader.IsDBNull(10) ? null : reader.GetString(10),
            MedicalHistory = reader.IsDBNull(11) ? null : reader.GetString(11),
            OphthalmicHistory = reader.IsDBNull(12) ? null : reader.GetString(12),
            UsesGlasses = reader.GetInt32(13) == 1,
            ContactLenses = reader.GetInt32(14) == 1,
            Allergies = reader.IsDBNull(15) ? null : reader.GetString(15),
            CurrentMedications = reader.IsDBNull(16) ? null : reader.GetString(16),
            GeneralNotes = reader.IsDBNull(17) ? null : reader.GetString(17),
            IsActive = reader.GetInt32(18) == 1,
            CreatedAt = DateTime.Parse(reader.GetString(19))
        };
    }
}

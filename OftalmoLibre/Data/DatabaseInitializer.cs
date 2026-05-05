using OftalmoLibre.Helpers;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Data;

public static class DatabaseInitializer
{
    public static void Initialize()
    {
        DbPaths.EnsureDirectories();
        CreateSchema();
        EnsureAppointmentColumns();
        EnsureCenterConfig();
        EnsureDefaultCatalogData();
        EnsureDefaultAdmin();
    }

    private static void CreateSchema()
    {
        Database.Execute(
            """
            CREATE TABLE IF NOT EXISTS users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                username TEXT NOT NULL UNIQUE,
                full_name TEXT NOT NULL,
                password_hash TEXT NOT NULL,
                role TEXT NOT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                must_change_password INTEGER NOT NULL DEFAULT 0,
                created_at TEXT NOT NULL,
                updated_at TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS patients (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                record_number TEXT NOT NULL UNIQUE,
                full_name TEXT NOT NULL,
                document_number TEXT NULL UNIQUE,
                birth_date TEXT NULL,
                phone1 TEXT NULL,
                phone2 TEXT NULL,
                email TEXT NULL,
                address TEXT NULL,
                insurance TEXT NULL,
                occupation TEXT NULL,
                medical_history TEXT NULL,
                ophthalmic_history TEXT NULL,
                uses_glasses INTEGER NOT NULL DEFAULT 0,
                contact_lenses INTEGER NOT NULL DEFAULT 0,
                allergies TEXT NULL,
                current_medications TEXT NULL,
                general_notes TEXT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS professionals (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                full_name TEXT NOT NULL,
                professional_type TEXT NOT NULL,
                specialty TEXT NOT NULL,
                registration_number TEXT NULL,
                phone TEXT NULL,
                email TEXT NULL,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS services (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT NULL,
                price REAL NOT NULL DEFAULT 0,
                duration_minutes INTEGER NOT NULL DEFAULT 30,
                is_active INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS boxes (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL UNIQUE,
                is_active INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS appointments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id INTEGER NOT NULL,
                professional_id INTEGER NOT NULL,
                service_id INTEGER NOT NULL,
                scheduled_at TEXT NOT NULL,
                end_at TEXT NULL,
                status TEXT NOT NULL,
                payment_status TEXT NOT NULL DEFAULT 'No Pagado',
                agenda TEXT NULL,
                notes TEXT NULL,
                created_at TEXT NOT NULL,
                FOREIGN KEY(patient_id) REFERENCES patients(id),
                FOREIGN KEY(professional_id) REFERENCES professionals(id),
                FOREIGN KEY(service_id) REFERENCES services(id)
            );

            CREATE TABLE IF NOT EXISTS attentions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id INTEGER NOT NULL,
                professional_id INTEGER NOT NULL,
                appointment_id INTEGER NULL,
                visit_date TEXT NOT NULL,
                chief_complaint TEXT NULL,
                clinical_notes TEXT NULL,
                plan TEXT NULL,
                visual_acuity_right TEXT NULL,
                visual_acuity_left TEXT NULL,
                FOREIGN KEY(patient_id) REFERENCES patients(id),
                FOREIGN KEY(professional_id) REFERENCES professionals(id),
                FOREIGN KEY(appointment_id) REFERENCES appointments(id)
            );

            CREATE TABLE IF NOT EXISTS prescriptions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id INTEGER NOT NULL,
                professional_id INTEGER NOT NULL,
                attention_id INTEGER NULL,
                prescription_date TEXT NOT NULL,
                sphere_right TEXT NULL,
                cylinder_right TEXT NULL,
                axis_right TEXT NULL,
                sphere_left TEXT NULL,
                cylinder_left TEXT NULL,
                axis_left TEXT NULL,
                add_power TEXT NULL,
                pupillary_distance TEXT NULL,
                notes TEXT NULL,
                FOREIGN KEY(patient_id) REFERENCES patients(id),
                FOREIGN KEY(professional_id) REFERENCES professionals(id),
                FOREIGN KEY(attention_id) REFERENCES attentions(id)
            );

            CREATE TABLE IF NOT EXISTS eye_exams (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id INTEGER NOT NULL,
                professional_id INTEGER NOT NULL,
                attention_id INTEGER NULL,
                exam_date TEXT NOT NULL,
                exam_type TEXT NOT NULL,
                result_summary TEXT NULL,
                notes TEXT NULL,
                FOREIGN KEY(patient_id) REFERENCES patients(id),
                FOREIGN KEY(professional_id) REFERENCES professionals(id),
                FOREIGN KEY(attention_id) REFERENCES attentions(id)
            );

            CREATE TABLE IF NOT EXISTS diagnoses (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id INTEGER NOT NULL,
                professional_id INTEGER NOT NULL,
                attention_id INTEGER NULL,
                diagnosis_date TEXT NOT NULL,
                code TEXT NULL,
                description TEXT NOT NULL,
                notes TEXT NULL,
                FOREIGN KEY(patient_id) REFERENCES patients(id),
                FOREIGN KEY(professional_id) REFERENCES professionals(id),
                FOREIGN KEY(attention_id) REFERENCES attentions(id)
            );

            CREATE TABLE IF NOT EXISTS payments (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id INTEGER NOT NULL,
                appointment_id INTEGER NULL,
                attention_id INTEGER NULL,
                payment_date TEXT NOT NULL,
                amount REAL NOT NULL,
                method TEXT NOT NULL,
                reference TEXT NULL,
                notes TEXT NULL,
                FOREIGN KEY(patient_id) REFERENCES patients(id),
                FOREIGN KEY(appointment_id) REFERENCES appointments(id),
                FOREIGN KEY(attention_id) REFERENCES attentions(id)
            );

            CREATE TABLE IF NOT EXISTS center_config (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                center_name TEXT NOT NULL,
                address TEXT NULL,
                phone TEXT NULL,
                email TEXT NULL,
                default_currency TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS audit_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                user_id INTEGER NOT NULL,
                action TEXT NOT NULL,
                entity_name TEXT NOT NULL,
                entity_id TEXT NULL,
                details TEXT NULL,
                created_at TEXT NOT NULL,
                FOREIGN KEY(user_id) REFERENCES users(id)
            );

            CREATE TABLE IF NOT EXISTS backup_records (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                file_name TEXT NOT NULL,
                full_path TEXT NOT NULL,
                created_at TEXT NOT NULL,
                notes TEXT NULL
            );
            """);
    }

    private static void EnsureAppointmentColumns()
    {
        EnsureColumn("appointments", "end_at", "TEXT NULL");
        EnsureColumn("appointments", "payment_status", "TEXT NOT NULL DEFAULT 'No Pagado'");
        EnsureColumn("appointments", "agenda", "TEXT NULL");

        Database.Execute(
            """
            UPDATE appointments
            SET end_at = datetime(scheduled_at, '+15 minutes')
            WHERE end_at IS NULL OR trim(end_at) = '';
            """);

        Database.Execute(
            """
            UPDATE appointments
            SET payment_status = 'No Pagado'
            WHERE payment_status IS NULL OR trim(payment_status) = '';
            """);

        Database.Execute(
            """
            UPDATE appointments
            SET agenda = 'BOX CONDELL'
            WHERE agenda IS NULL OR trim(agenda) = '';
            """);
    }

    private static void EnsureColumn(string tableName, string columnName, string columnDefinition)
    {
        var columns = Database.Query(
            $"PRAGMA table_info({tableName});",
            reader => reader.GetString(1));

        if (columns.Any(x => string.Equals(x, columnName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        Database.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
    }

    private static void EnsureCenterConfig()
    {
        var centerName = Convert.ToString(Database.Scalar("SELECT center_name FROM center_config WHERE id = 1 LIMIT 1;"));
        if (string.IsNullOrWhiteSpace(centerName))
        {
            Database.Execute(
                """
                INSERT INTO center_config (id, center_name, address, phone, email, default_currency, updated_at)
                VALUES (1, @center_name, @address, @phone, @email, @default_currency, @updated_at);
                """,
                new Dictionary<string, object?>
                {
                    ["@center_name"] = AppIdentity.DefaultCenterName,
                    ["@address"] = "Chile Chico",
                    ["@phone"] = string.Empty,
                    ["@email"] = string.Empty,
                    ["@default_currency"] = "CLP",
                    ["@updated_at"] = DateTime.Now.ToString("s")
                });
            return;
        }

        if (!string.Equals(centerName, "OftalmoLibre", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        Database.Execute(
            """
            UPDATE center_config
            SET center_name = @center_name,
                updated_at = @updated_at
            WHERE id = 1;
            """,
            new Dictionary<string, object?>
            {
                ["@center_name"] = AppIdentity.DefaultCenterName,
                ["@updated_at"] = DateTime.Now.ToString("s")
            });
    }

    private static void EnsureDefaultCatalogData()
    {
        var serviceCount = Convert.ToInt64(Database.Scalar("SELECT COUNT(*) FROM services;") ?? 0L);
        if (serviceCount == 0)
        {
            Database.Execute(
                """
                INSERT INTO services (name, description, price, duration_minutes, is_active)
                VALUES (@name, @description, @price, @duration_minutes, 1);
                """,
                new Dictionary<string, object?>
                {
                    ["@name"] = "CONSULTA Tecnólogo Médico",
                    ["@description"] = "Atención visual de la óptica",
                    ["@price"] = 15000m,
                    ["@duration_minutes"] = 15
                });
        }

        var professionalCount = Convert.ToInt64(Database.Scalar("SELECT COUNT(*) FROM professionals;") ?? 0L);
        if (professionalCount == 0)
        {
            Database.Execute(
                """
                INSERT INTO professionals (full_name, professional_type, specialty, registration_number, phone, email, is_active, created_at)
                VALUES (@full_name, @professional_type, @specialty, @registration_number, @phone, @email, 1, @created_at);
                """,
                new Dictionary<string, object?>
                {
                    ["@full_name"] = "TMO. FRANCHESCA CATALINA VARGAS",
                    ["@professional_type"] = "Tecnólogo Médico",
                    ["@specialty"] = "Atención visual",
                    ["@registration_number"] = "TMO-001",
                    ["@phone"] = string.Empty,
                    ["@email"] = string.Empty,
                    ["@created_at"] = DateTime.Now.ToString("s")
                });
        }

        var boxCount = Convert.ToInt64(Database.Scalar("SELECT COUNT(*) FROM boxes;") ?? 0L);
        if (boxCount == 0)
        {
            Database.Execute(
                """
                INSERT INTO boxes (name, is_active)
                VALUES ('BOX CONDELL', 1);
                """);
        }
    }

    private static void EnsureDefaultAdmin()
    {
        var userRepository = new UserRepository();
        if (userRepository.AnyUsers())
        {
            return;
        }

        userRepository.CreateInitialAdministrator("admin", "Administrador", PasswordHelper.HashPassword("admin123"));
    }
}

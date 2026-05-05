using System.Data;
using System.Security.Cryptography;
using OftalmoLibre.Data;
using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

var runner = new SmokeTestRunner();
return await runner.RunAsync();

internal sealed class SmokeTestRunner
{
    private readonly List<string> _completedChecks = [];
    private readonly DateTime _now = DateTime.Now;
    private string _testDirectory = string.Empty;

    public Task<int> RunAsync()
    {
        try
        {
            PrepareIsolatedEnvironment();

            DatabaseInitializer.Initialize();

            CheckPasswordCompatibility();
            var adminUser = CheckInitializationAndAdmin();
            var centerConfig = CheckCenterConfiguration();
            var professional = CheckProfessionals();
            var patient = CheckPatients();
            var service = CheckServices();
            var box = CheckBoxes();
            var appointments = CheckAppointments(patient, professional, service, box);
            CheckBoxRename(box, appointments[0].Id);
            var attention = CheckAttentions(patient, professional, appointments[0].Id);
            var prescription = CheckPrescriptions(patient, professional, attention.Id);
            var exam = CheckExams(patient, professional, attention.Id);
            var diagnosis = CheckDiagnoses(patient, professional, attention.Id);
            var payment = CheckPayments(patient, appointments[1].Id, attention.Id);
            CheckReports(payment.Amount);
            CheckAudit(adminUser.Id, patient.Id);
            CheckBackup();
            CheckExport();

            Console.WriteLine("PRUEBA COMPLETA OK");
            Console.WriteLine($"Base temporal: {_testDirectory}");
            Console.WriteLine("Chequeos ejecutados:");
            foreach (var check in _completedChecks)
            {
                Console.WriteLine($" - {check}");
            }

            return Task.FromResult(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("PRUEBA FALLIDA");
            Console.Error.WriteLine(ex.ToString());
            Console.Error.WriteLine($"Base temporal: {_testDirectory}");
            return Task.FromResult(1);
        }
    }

    private void PrepareIsolatedEnvironment()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "OftalmoLibre.SmokeTests", DateTime.Now.ToString("yyyyMMdd_HHmmss"));

        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }

        Environment.SetEnvironmentVariable("OFTALMOLIBRE_APPDIR", _testDirectory);
    }

    private void CheckPasswordCompatibility()
    {
        const string password = "admin123";

        var generatedHash = PasswordHelper.HashPassword(password);
        Expect(PasswordHelper.VerifyPassword(password, generatedHash), "La contraseña generada debe validarse correctamente.");
        Expect(!PasswordHelper.VerifyPassword("otra-clave", generatedHash), "Una contraseña incorrecta no debe validarse.");

        var legacySalt = Enumerable.Range(1, 16).Select(i => (byte)i).ToArray();
        var legacyHashBytes = Rfc2898DeriveBytes.Pbkdf2(password, legacySalt, 100_000, HashAlgorithmName.SHA256, 32);
        var legacyHash = $"100000.{Convert.ToBase64String(legacySalt)}.{Convert.ToBase64String(legacyHashBytes)}";

        Expect(PasswordHelper.VerifyPassword(password, legacyHash), "El helper debe seguir validando hashes PBKDF2-SHA256 existentes.");

        _completedChecks.Add("Compatibilidad de contraseñas");
    }

    private User CheckInitializationAndAdmin()
    {
        Expect(File.Exists(DbPaths.DatabasePath), "La base SQLite debe crearse al inicializar.");

        var userRepository = new UserRepository();
        Expect(userRepository.AnyUsers(), "Debe existir al menos un usuario inicial.");

        var authService = new AuthService();
        var adminUser = authService.Login("admin", "admin123");
        Expect(adminUser is not null, "El usuario admin inicial debe autenticarse.");
        Expect(adminUser!.Role == "Administrador", "El admin inicial debe tener rol Administrador.");
        Expect(adminUser.MustChangePassword, "El admin inicial debe marcar cambio de contraseña.");
        Expect(authService.Login("admin", "incorrecta") is null, "La contraseña incorrecta no debe autenticar.");
        Expect(authService.CanAccess("Recepción", "Pacientes"), "Recepción debe poder acceder a clientes.");
        Expect(!authService.CanAccess("Recepción", "Usuarios"), "Recepción no debe poder acceder a usuarios.");

        _completedChecks.Add("Inicialización, login y permisos");
        return adminUser;
    }

    private CenterConfig CheckCenterConfiguration()
    {
        var repository = new CenterConfigRepository();
        var config = repository.Get();

        Expect(config.CenterName == AppIdentity.DefaultCenterName, "La óptica por defecto debe quedar configurada.");

        config.Phone = "+56 9 1234 5678";
        config.Email = "contacto@opticaimagen.cl";
        repository.Save(config);

        var saved = repository.Get();
        Expect(saved.Phone == config.Phone, "La configuración debe actualizar teléfono.");
        Expect(saved.Email == config.Email, "La configuración debe actualizar correo.");

        _completedChecks.Add("Configuración del centro");
        return saved;
    }

    private Professional CheckProfessionals()
    {
        var repository = new ProfessionalRepository();
        var professional = new Professional
        {
            FullName = "Franchesca Vargas",
            ProfessionalType = "Tecnólogo Médico",
            Specialty = "Optometría",
            RegistrationNumber = "TM-001",
            Phone = "+56 9 5555 1111",
            Email = "franchesca@opticaimagen.cl",
            IsActive = true
        };

        repository.Save(professional);
        Expect(professional.Id > 0, "El profesional debe persistirse con Id.");

        professional.Specialty = "Atención visual";
        repository.Save(professional);

        var saved = repository.GetById(professional.Id);
        Expect(saved is not null, "El profesional creado debe recuperarse.");
        Expect(saved!.Specialty == "Atención visual", "La edición del profesional debe persistirse.");
        Expect(repository.GetActive().Any(x => x.Id == professional.Id), "El profesional activo debe aparecer en lista activa.");

        _completedChecks.Add("Profesionales");
        return saved;
    }

    private Patient CheckPatients()
    {
        var repository = new PatientRepository();
        var patient = new Patient
        {
            FullName = "Javiera Vas Muñoz",
            DocumentNumber = "19817115-5",
            BirthDate = new DateTime(1994, 5, 14),
            Phone1 = "+56 9 6516 3633",
            Email = "javiera@example.com",
            Address = "Chile Chico",
            UsesGlasses = true,
            GeneralNotes = "Cliente frecuente",
            IsActive = true
        };

        repository.Save(patient);
        Expect(patient.Id > 0, "El cliente debe persistirse con Id.");
        Expect(patient.RecordNumber.StartsWith("CLI-", StringComparison.Ordinal), "La ficha del cliente debe usar prefijo CLI.");
        Expect(repository.ExistsDocument(patient.DocumentNumber), "El documento debe detectarse como existente.");

        patient.GeneralNotes = "Cliente frecuente de control anual";
        repository.Save(patient);

        var saved = repository.GetById(patient.Id);
        Expect(saved is not null, "El cliente creado debe recuperarse.");
        Expect(saved!.GeneralNotes == patient.GeneralNotes, "La edición del cliente debe persistirse.");
        Expect(repository.GetAll("Javiera", includeInactive: false).Any(x => x.Id == patient.Id), "La búsqueda de clientes debe encontrar coincidencias.");
        Expect(repository.GetByDocumentNumber("198171155")?.Id == patient.Id, "El cliente debe encontrarse por RUT aunque se escriba sin guion.");
        Expect(saved.Age > 0, "La edad calculada debe ser mayor a cero.");
        Expect(ValidationHelper.IsValidEmail(saved.Email), "El correo guardado debe ser válido.");

        _completedChecks.Add("Clientes");
        return saved;
    }

    private OphthalmologyService CheckServices()
    {
        var repository = new ServiceRepository();
        var service = new OphthalmologyService
        {
            Name = "Control visual",
            Description = "Atención base de óptica",
            Price = 15000m,
            DurationMinutes = 20,
            IsActive = true
        };

        repository.Save(service);
        Expect(service.Id > 0, "La prestación debe persistirse con Id.");

        service.Price = 18000m;
        repository.Save(service);

        var saved = repository.GetById(service.Id);
        Expect(saved is not null, "La prestación creada debe recuperarse.");
        Expect(saved!.Price == 18000m, "La edición de la prestación debe persistirse.");
        Expect(repository.GetActive().Any(x => x.Id == service.Id), "La prestación activa debe aparecer en lista activa.");

        _completedChecks.Add("Prestaciones");
        return saved;
    }

    private BoxLocation CheckBoxes()
    {
        var repository = new BoxRepository();
        var box = new BoxLocation
        {
            Name = "BOX EXAMEN 1",
            IsActive = true
        };

        repository.Save(box);
        Expect(box.Id > 0, "El box debe persistirse con Id.");

        var saved = repository.GetById(box.Id);
        Expect(saved is not null, "El box creado debe recuperarse.");
        Expect(repository.GetActiveNames().Any(x => x == box.Name), "El box activo debe aparecer en la lista de selección.");

        _completedChecks.Add("Boxes");
        return saved!;
    }

    private List<Appointment> CheckAppointments(Patient patient, Professional professional, OphthalmologyService service, BoxLocation box)
    {
        var repository = new AppointmentRepository();
        var appointmentService = new AppointmentService();

        var items = new[]
        {
            new Appointment { PatientId = patient.Id, ProfessionalId = professional.Id, ServiceId = service.Id, ScheduledAt = _now.AddHours(1), EndAt = _now.AddHours(1).AddMinutes(20), Status = "Pendiente", PaymentStatus = "No Pagado", Agenda = box.Name, Notes = "Primera hora" },
            new Appointment { PatientId = patient.Id, ProfessionalId = professional.Id, ServiceId = service.Id, ScheduledAt = _now.AddHours(2), EndAt = _now.AddHours(2).AddMinutes(20), Status = "Confirmada", PaymentStatus = "Pagado", Agenda = "BOX CONDELL", Notes = "Confirmada por teléfono" },
            new Appointment { PatientId = patient.Id, ProfessionalId = professional.Id, ServiceId = service.Id, ScheduledAt = _now.AddHours(3), EndAt = _now.AddHours(3).AddMinutes(20), Status = "Atendida", PaymentStatus = "Pagado", Agenda = box.Name, Notes = "Atención realizada" },
            new Appointment { PatientId = patient.Id, ProfessionalId = professional.Id, ServiceId = service.Id, ScheduledAt = _now.AddHours(4), EndAt = _now.AddHours(4).AddMinutes(20), Status = "Cancelada", PaymentStatus = "No Pagado", Agenda = box.Name, Notes = "Cliente reagenda" }
        };

        foreach (var item in items)
        {
            appointmentService.Save(item);
            Expect(item.Id > 0, "Cada cita debe persistirse con Id.");
        }

        items[0].Status = "Confirmada";
        repository.Save(items[0]);

        var allToday = repository.GetAll(_now.Date);
        Expect(allToday.Count == 4, "Las citas del día deben recuperarse completas.");
        Expect(allToday.Any(x => x.Agenda == box.Name), "Las citas deben conservar el box guardado.");
        Expect(allToday.Any(x => x.PaymentStatus == "Pagado"), "Las citas deben conservar el estado de pago.");
        Expect(allToday.All(x => x.EndAt.HasValue), "Las citas deben conservar la hora de término.");
        Expect(repository.GetByPatient(patient.Id).Count == 4, "Las citas por cliente deben recuperarse.");
        Expect(repository.GetUpcoming(10).Count >= 4, "Las próximas citas deben recuperarse.");
        Expect(appointmentService.Statuses.Contains("Atendida"), "El catálogo de estados debe contener Atendida.");

        var counts = repository.GetStatusCountsForDay(_now.Date);
        Expect(counts.TryGetValue("Total", out var total) && total == 4, "El total de citas del día debe ser correcto.");
        Expect(counts.TryGetValue("Confirmada", out var confirmed) && confirmed == 2, "Las citas confirmadas del día deben ser correctas.");
        Expect(counts.TryGetValue("Atendida", out var attended) && attended == 1, "Las citas atendidas del día deben ser correctas.");
        Expect(counts.TryGetValue("Cancelada", out var cancelled) && cancelled == 1, "Las citas canceladas del día deben ser correctas.");

        _completedChecks.Add("Citas y estados");
        return items.ToList();
    }

    private void CheckBoxRename(BoxLocation box, int appointmentId)
    {
        var boxRepository = new BoxRepository();
        var appointmentRepository = new AppointmentRepository();
        var originalName = box.Name;

        box.Name = "BOX CONTROL 2";
        boxRepository.Save(box, originalName);

        var updatedAppointment = appointmentRepository.GetById(appointmentId);
        Expect(updatedAppointment is not null, "La cita debe seguir disponible tras renombrar el box.");
        Expect(updatedAppointment!.Agenda == box.Name, "Renombrar el box debe actualizar las citas existentes.");
        Expect(boxRepository.GetByName("box control 2")?.Id == box.Id, "El box renombrado debe poder buscarse por nombre.");

        _completedChecks.Add("Renombre de box");
    }

    private Attention CheckAttentions(Patient patient, Professional professional, int appointmentId)
    {
        var repository = new AttentionRepository();
        var attention = new Attention
        {
            PatientId = patient.Id,
            ProfessionalId = professional.Id,
            AppointmentId = appointmentId,
            VisitDate = _now,
            ChiefComplaint = "Visión borrosa",
            ClinicalNotes = "Se evalúa refracción.",
            Plan = "Control en 6 meses",
            VisualAcuityRight = "20/25",
            VisualAcuityLeft = "20/20"
        };

        repository.Save(attention);
        Expect(attention.Id > 0, "La atención debe persistirse con Id.");

        attention.Plan = "Control en 12 meses";
        repository.Save(attention);

        var saved = repository.GetById(attention.Id);
        Expect(saved is not null, "La atención creada debe recuperarse.");
        Expect(saved!.Plan == "Control en 12 meses", "La edición de la atención debe persistirse.");
        Expect(repository.GetByPatient(patient.Id).Any(x => x.Id == attention.Id), "La atención debe aparecer en la ficha del cliente.");
        Expect(repository.GetByPatientForLookup(patient.Id).Any(x => x.Id == attention.Id), "La atención debe aparecer en el lookup del cliente.");

        _completedChecks.Add("Atenciones");
        return saved;
    }

    private OpticalPrescription CheckPrescriptions(Patient patient, Professional professional, int attentionId)
    {
        var repository = new PrescriptionRepository();
        var prescription = new OpticalPrescription
        {
            PatientId = patient.Id,
            ProfessionalId = professional.Id,
            AttentionId = attentionId,
            PrescriptionDate = _now,
            SphereRight = "-1.25",
            CylinderRight = "-0.50",
            AxisRight = "180",
            SphereLeft = "-1.00",
            CylinderLeft = "-0.25",
            AxisLeft = "170",
            AddPower = "+1.00",
            PupillaryDistance = "62",
            Notes = "Uso permanente"
        };

        repository.Save(prescription);
        Expect(prescription.Id > 0, "La receta debe persistirse con Id.");

        prescription.Notes = "Uso permanente, revisión anual";
        repository.Save(prescription);

        var saved = repository.GetById(prescription.Id);
        Expect(saved is not null, "La receta creada debe recuperarse.");
        Expect(saved!.Notes == prescription.Notes, "La edición de la receta debe persistirse.");
        Expect(repository.GetByPatient(patient.Id).Any(x => x.Id == prescription.Id), "La receta debe aparecer en la ficha del cliente.");
        Expect(repository.GetAll().Any(x => x.Id == prescription.Id), "La receta debe aparecer en el listado general.");
        Expect(repository.GetAll("198171155").Any(x => x.Id == prescription.Id), "La receta debe encontrarse por RUT sin formato.");
        Expect(repository.GetAll(patient.DocumentNumber).Any(x => x.Id == prescription.Id), "La receta debe encontrarse por RUT guardado.");

        _completedChecks.Add("Recetas");
        return saved;
    }

    private EyeExam CheckExams(Patient patient, Professional professional, int attentionId)
    {
        var repository = new ExamRepository();
        var exam = new EyeExam
        {
            PatientId = patient.Id,
            ProfessionalId = professional.Id,
            AttentionId = attentionId,
            ExamDate = _now,
            ExamType = "Tonometría",
            ResultSummary = "Dentro de parámetros",
            Notes = "Sin hallazgos"
        };

        repository.Save(exam);
        Expect(exam.Id > 0, "El examen debe persistirse con Id.");

        exam.Notes = "Sin hallazgos relevantes";
        repository.Save(exam);

        var saved = repository.GetById(exam.Id);
        Expect(saved is not null, "El examen creado debe recuperarse.");
        Expect(saved!.Notes == exam.Notes, "La edición del examen debe persistirse.");
        Expect(repository.GetByPatient(patient.Id).Any(x => x.Id == exam.Id), "El examen debe aparecer en la ficha del cliente.");

        _completedChecks.Add("Exámenes");
        return saved;
    }

    private Diagnosis CheckDiagnoses(Patient patient, Professional professional, int attentionId)
    {
        var repository = new DiagnosisRepository();
        var diagnosis = new Diagnosis
        {
            PatientId = patient.Id,
            ProfessionalId = professional.Id,
            AttentionId = attentionId,
            DiagnosisDate = _now,
            Code = "H52.1",
            Description = "Miopía",
            Notes = "Leve"
        };

        repository.Save(diagnosis);
        Expect(diagnosis.Id > 0, "El diagnóstico debe persistirse con Id.");

        diagnosis.Notes = "Leve bilateral";
        repository.Save(diagnosis);

        var saved = repository.GetById(diagnosis.Id);
        Expect(saved is not null, "El diagnóstico creado debe recuperarse.");
        Expect(saved!.Notes == diagnosis.Notes, "La edición del diagnóstico debe persistirse.");
        Expect(repository.GetByPatient(patient.Id).Any(x => x.Id == diagnosis.Id), "El diagnóstico debe aparecer en la ficha del cliente.");

        _completedChecks.Add("Diagnósticos");
        return saved;
    }

    private Payment CheckPayments(Patient patient, int appointmentId, int attentionId)
    {
        var repository = new PaymentRepository();
        var payment = new Payment
        {
            PatientId = patient.Id,
            AppointmentId = appointmentId,
            AttentionId = attentionId,
            PaymentDate = _now,
            Amount = 18000m,
            Method = "Transferencia",
            Reference = "TRX-001",
            Notes = "Pago completo"
        };

        repository.Save(payment);
        Expect(payment.Id > 0, "El pago debe persistirse con Id.");

        payment.Reference = "TRX-002";
        repository.Save(payment);

        var saved = repository.GetById(payment.Id);
        Expect(saved is not null, "El pago creado debe recuperarse.");
        Expect(saved!.Reference == payment.Reference, "La edición del pago debe persistirse.");
        Expect(repository.GetByPatient(patient.Id).Any(x => x.Id == payment.Id), "El pago debe aparecer en la ficha del cliente.");
        Expect(repository.GetTotalByDay(_now.Date) == payment.Amount, "El total diario de pagos debe coincidir.");

        _completedChecks.Add("Pagos");
        return saved;
    }

    private void CheckReports(decimal expectedRevenue)
    {
        var reportService = new ReportService();
        var snapshot = reportService.GetDashboardSnapshot();

        Expect(snapshot.AppointmentsToday == 4, "El dashboard debe contar cuatro citas hoy.");
        Expect(snapshot.ConfirmedAppointments == 2, "El dashboard debe contar dos citas confirmadas.");
        Expect(snapshot.CompletedAppointments == 1, "El dashboard debe contar una cita atendida.");
        Expect(snapshot.CancelledAppointments == 1, "El dashboard debe contar una cita cancelada.");
        Expect(snapshot.RevenueToday == expectedRevenue, "El dashboard debe calcular el ingreso diario.");
        Expect(snapshot.UpcomingAppointments.Count >= 4, "El dashboard debe listar próximas citas.");

        var paymentsReport = reportService.GetPaymentsReport(_now.Date, _now.Date);
        var appointmentsReport = reportService.GetAppointmentsReport(_now.Date, _now.Date);

        Expect(paymentsReport.Rows.Count == 1, "El reporte de pagos debe incluir el pago del día.");
        Expect(appointmentsReport.Rows.Count == 4, "El reporte de citas debe incluir todas las citas del día.");

        _completedChecks.Add("Reportes y dashboard");
    }

    private void CheckAudit(int userId, int patientId)
    {
        var service = new AuditService();
        var repository = new AuditRepository();

        service.Log(userId, "Actualizar", "Cliente", patientId.ToString(), "Prueba automática");

        var items = repository.GetRecent(10);
        Expect(items.Any(x => x.UserId == userId && x.EntityName == "Cliente"), "La auditoría debe registrar acciones.");

        _completedChecks.Add("Auditoría");
    }

    private void CheckBackup()
    {
        var service = new BackupService();
        var record = service.CreateBackup("Smoke test");

        Expect(record.Id > 0, "El backup debe registrarse con Id.");
        Expect(File.Exists(record.FullPath), "El archivo de backup debe existir.");
        Expect(service.GetBackups().Any(x => x.Id == record.Id), "El backup debe aparecer en el listado.");

        _completedChecks.Add("Backups");
    }

    private void CheckExport()
    {
        var exportService = new ExportService();
        var filePath = Path.Combine(_testDirectory, "reporte.csv");
        var table = new DataTable();
        table.Columns.Add("Nombre");
        table.Columns.Add("Valor");
        table.Rows.Add("Clientes", "1");

        exportService.ExportDataTable(table, filePath);

        Expect(File.Exists(filePath), "La exportación CSV debe crear el archivo.");
        var rawBytes = File.ReadAllBytes(filePath);
        Expect(rawBytes.Length >= 3 && rawBytes[0] == 0xEF && rawBytes[1] == 0xBB && rawBytes[2] == 0xBF,
            "El CSV debe incluir BOM UTF-8 para que Excel en Windows abra los caracteres correctamente.");
        var contents = File.ReadAllText(filePath);
        Expect(contents.Contains("\"Clientes\"", StringComparison.Ordinal), "La exportación CSV debe incluir los datos.");

        _completedChecks.Add("Exportación CSV");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}

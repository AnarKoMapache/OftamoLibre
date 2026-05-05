Quiero que desarrolles un software libre para Windows llamado “OftalmoLibre”.

Debe ser un programa de escritorio simple, rápido y liviano para gestión de atención oftalmológica.

El sistema debe estar enfocado exclusivamente en:
- Centros oftalmológicos
- Consultas oftalmológicas
- Tecnólogos médicos en oftalmología
- Médicos oftalmólogos
- Ópticas con atención visual
- Centros que realizan evaluaciones, controles, recetas ópticas y exámenes oftalmológicos

El software debe ser de código abierto, bajo licencia GNU GPLv3.

Objetivo general:
Crear una aplicación de escritorio para Windows que permita administrar pacientes, agenda, citas, profesionales, prestaciones oftalmológicas, atenciones clínicas básicas, recetas ópticas, exámenes, diagnósticos, pagos, reportes, backups y usuarios.

Debe funcionar localmente, sin internet, sin servidor web y sin depender de servicios externos.

Stack obligatorio:
- Lenguaje: C#
- Plataforma: .NET 8
- Interfaz: WinForms
- Base de datos: SQLite local
- Acceso a datos: Microsoft.Data.Sqlite, evitando complejidad innecesaria
- Sistema operativo principal: Windows 10 y Windows 11
- Arquitectura simple, modular y fácil de mantener
- Licencia: GNU GPLv3

Restricciones importantes:
- No usar servidor web
- No usar Docker
- No usar FastAPI
- No usar tecnologías web
- No usar WPF
- No usar MAUI
- No usar dependencias innecesariamente complejas
- No generar solo explicación: generar código funcional
- El sistema debe compilar como aplicación Windows .exe
- La interfaz debe estar completamente en español
- El sistema debe crear un usuario administrador inicial si no existe
- La aplicación debe poder abrirse, iniciar sesión y operar con datos reales

Nombre del proyecto:
OftalmoLibre

Nombre técnico de la solución:
OftalmoLibre.sln

Tipo:
Aplicación Windows Forms en C# .NET 8 con SQLite local.

Prioridades:
1. Simple
2. Rápido
3. Fácil de usar
4. Fácil de instalar
5. Código ordenado
6. Interfaz clara
7. Funcional antes que excesivamente compleja
8. Pensado para recepción, tecnólogos médicos y oftalmólogos

Estructura sugerida del proyecto:

OftalmoLibre/
│
├── OftalmoLibre.sln
├── README.md
├── LICENSE
│
└── OftalmoLibre/
    ├── OftalmoLibre.csproj
    ├── Program.cs
    │
    ├── Data/
    │   ├── Database.cs
    │   ├── DatabaseInitializer.cs
    │   └── DbPaths.cs
    │
    ├── Models/
    │   ├── User.cs
    │   ├── Patient.cs
    │   ├── Professional.cs
    │   ├── OphthalmologyService.cs
    │   ├── Appointment.cs
    │   ├── Attention.cs
    │   ├── OpticalPrescription.cs
    │   ├── EyeExam.cs
    │   ├── Diagnosis.cs
    │   ├── Payment.cs
    │   ├── CenterConfig.cs
    │   ├── AuditLog.cs
    │   └── BackupRecord.cs
    │
    ├── Repositories/
    │   ├── UserRepository.cs
    │   ├── PatientRepository.cs
    │   ├── ProfessionalRepository.cs
    │   ├── ServiceRepository.cs
    │   ├── AppointmentRepository.cs
    │   ├── AttentionRepository.cs
    │   ├── PrescriptionRepository.cs
    │   ├── ExamRepository.cs
    │   ├── DiagnosisRepository.cs
    │   ├── PaymentRepository.cs
    │   └── AuditRepository.cs
    │
    ├── Services/
    │   ├── AuthService.cs
    │   ├── BackupService.cs
    │   ├── ReportService.cs
    │   ├── ExportService.cs
    │   ├── AppointmentService.cs
    │   └── AuditService.cs
    │
    ├── Forms/
    │   ├── LoginForm.cs
    │   ├── MainForm.cs
    │   ├── DashboardForm.cs
    │   ├── AgendaForm.cs
    │   ├── AppointmentDetailForm.cs
    │   ├── PatientsForm.cs
    │   ├── PatientEditorForm.cs
    │   ├── PatientProfileForm.cs
    │   ├── ProfessionalsForm.cs
    │   ├── ServicesForm.cs
    │   ├── AttentionsForm.cs
    │   ├── AttentionEditorForm.cs
    │   ├── PrescriptionsForm.cs
    │   ├── PrescriptionEditorForm.cs
    │   ├── ExamsForm.cs
    │   ├── ExamEditorForm.cs
    │   ├── DiagnosesForm.cs
    │   ├── PaymentsForm.cs
    │   ├── PaymentForm.cs
    │   ├── ReportsForm.cs
    │   ├── UsersForm.cs
    │   ├── SettingsForm.cs
    │   └── BackupsForm.cs
    │
    ├── Helpers/
    │   ├── PasswordHelper.cs
    │   ├── DateHelper.cs
    │   ├── ValidationHelper.cs
    │   ├── UiHelper.cs
    │   └── CsvHelper.cs
    │
    ├── Resources/
    │
    └── Backups/

Genera todos los formularios de WinForms usando código C# directamente o incluye todos los archivos necesarios .Designer.cs si decides usar Designer. No dejes archivos incompletos.

El programa debe iniciar así:
1. Program.cs abre LoginForm.
2. LoginForm valida usuario y contraseña.
3. Si el login es correcto, abre MainForm.
4. MainForm contiene menú lateral o superior.
5. Desde MainForm se abren los módulos principales.
6. El usuario puede cerrar sesión.

Usuario administrador inicial:
Al iniciar la aplicación por primera vez, si no existen usuarios, crear automáticamente:

Usuario: admin
Contraseña: admin123
Rol: Administrador

Al iniciar sesión por primera vez, mostrar advertencia recomendando cambiar la contraseña.

Roles del sistema:
- Administrador
- Recepción
- Profesional
- Caja
- Solo lectura

Permisos:
Administrador:
- Acceso completo

Recepción:
- Dashboard
- Agenda
- Pacientes
- Citas
- Ver historial básico
- Sin acceso a configuración avanzada

Profesional:
- Agenda
- Pacientes
- Atenciones
- Recetas ópticas
- Exámenes
- Diagnósticos

Caja:
- Pagos
- Reportes de caja

Solo lectura:
- Solo puede visualizar información

MÓDULOS DEL SISTEMA

1. Dashboard

Debe mostrar:
- Citas del día
- Pacientes agendados hoy
- Citas pendientes
- Citas confirmadas
- Citas atendidas
- Citas canceladas
- Ingresos del día
- Próximas citas
- Botones rápidos:
  - Nueva cita
  - Nuevo paciente
  - Nueva atención
  - Nueva receta óptica
  - Registrar pago

Diseño:
- Tarjetas simples
- Botones grandes
- Tabla con próximas citas
- Actualización rápida al abrir

2. Pacientes

Debe permitir:
- Crear paciente
- Editar paciente
- Buscar paciente
- Desactivar paciente
- Ver ficha del paciente
- Ver historial de citas
- Ver historial de atenciones
- Ver historial de recetas ópticas
- Ver historial de exámenes
- Ver historial de pagos

Campos del paciente:
- Id
- Número de ficha
- Nombre completo
- RUT/DNI
- Fecha de nacimiento
- Edad calculada automáticamente
- Teléfono 1
- Teléfono 2
- Correo electrónico
- Dirección
- Previsión o seguro
- Ocupación
- Antecedentes médicos
- Antecedentes oftalmológicos
- Uso actual de lentes
- Uso de lentes de contacto
- Alergias
- Medicamentos actuales
- Observaciones generales
- Estado activo/inactivo
- Fecha de creación

Validaciones:
- Nombre obligatorio
- RUT/DNI opcional pero no duplicado si se ingresa
- Teléfono opcional
- Correo debe tener formato válido si se ingresa
- No eliminar pacientes con historial; solo desactivar

3. Profesionales

Debe permitir:
- Crear profesional
- Editar profesional
- Activar profesional
- Desactivar profesional
- Buscar profesional

Campos:
- Id
- Nombre completo
- Tipo de profesional
- Especialidad
- Registro profesional opcional
- Teléfono
- Correo
 profesional
- Especialidad
- Registro profesional opcional
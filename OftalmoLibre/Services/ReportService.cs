using System.Data;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Services;

public sealed class ReportService
{
    private readonly AppointmentRepository _appointmentRepository = new();
    private readonly PaymentRepository _paymentRepository = new();

    public DashboardSnapshot GetDashboardSnapshot()
    {
        var counts = _appointmentRepository.GetStatusCountsForDay(DateTime.Today);
        var upcoming = _appointmentRepository.GetUpcoming(10);

        return new DashboardSnapshot
        {
            AppointmentsToday = counts.TryGetValue("Total", out var total) ? total : 0,
            PendingAppointments = counts.TryGetValue("Pendiente", out var pending) ? pending : 0,
            ConfirmedAppointments = counts.TryGetValue("Confirmada", out var confirmed) ? confirmed : 0,
            CompletedAppointments = counts.TryGetValue("Atendida", out var completed) ? completed : 0,
            CancelledAppointments = counts.TryGetValue("Cancelada", out var cancelled) ? cancelled : 0,
            RevenueToday = _paymentRepository.GetTotalByDay(DateTime.Today),
            UpcomingAppointments = upcoming
        };
    }

    public DataTable GetPaymentsReport(DateTime from, DateTime to)
    {
        var table = new DataTable();
        table.Columns.Add("Fecha");
        table.Columns.Add("Paciente");
        table.Columns.Add("Monto");
        table.Columns.Add("Método");
        table.Columns.Add("Referencia");

        foreach (var item in _paymentRepository.GetDetailed(from, to))
        {
            table.Rows.Add(item.PaymentDate.ToString("g"), item.PatientName, item.Amount.ToString("N0"), item.Method, item.Reference);
        }

        return table;
    }

    public DataTable GetAppointmentsReport(DateTime from, DateTime to)
    {
        var table = new DataTable();
        table.Columns.Add("Fecha");
        table.Columns.Add("Paciente");
        table.Columns.Add("Profesional");
        table.Columns.Add("Prestación");
        table.Columns.Add("Estado");

        foreach (var item in _appointmentRepository.GetDetailed(from, to))
        {
            table.Rows.Add(item.ScheduledAt.ToString("g"), item.PatientName, item.ProfessionalName, item.ServiceName, item.Status);
        }

        return table;
    }
}

public sealed class DashboardSnapshot
{
    public int AppointmentsToday { get; set; }
    public int PendingAppointments { get; set; }
    public int ConfirmedAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public decimal RevenueToday { get; set; }
    public List<AppointmentListItem> UpcomingAppointments { get; set; } = [];
}

using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Forms;

public sealed class DashboardForm : Form
{
    private readonly Action<string, bool> _navigateAction;
    private readonly PatientRepository _patientRepository = new();
    private readonly ProfessionalRepository _professionalRepository = new();
    private readonly AttentionRepository _attentionRepository = new();
    private readonly PrescriptionRepository _prescriptionRepository = new();
    private readonly Dictionary<string, Label> _metricLabels = new();
    private readonly DataGridView _attentionsGrid = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _prescriptionsGrid = new() { Dock = DockStyle.Fill };

    public DashboardForm(User currentUser, Action<string, bool> navigateAction)
    {
        _navigateAction = navigateAction;

        Text = "Inicio";
        BackColor = Color.White;

        BuildLayout();
        Load += (_, _) => RefreshData();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel { Dock = DockStyle.Top, Height = 70 };
        header.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 40, 60),
            Height = 38,
            Text = "Panel de control"
        });
        header.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            ForeColor = Color.FromArgb(100, 110, 130),
            Height = 24,
            Text = $"Resumen del mes · {DateTime.Today:dddd d \\de MMMM \\de yyyy}"
        });

        var metrics = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 8),
            WrapContents = true
        };

        foreach (var (key, label, color) in new[]
                 {
                     ("Clientes activos",      "Clientes activos",     Color.FromArgb(33, 150, 243)),
                     ("Profesionales activos", "Profesionales activos",Color.FromArgb(76, 175, 80)),
                     ("Atenciones del mes",    "Atenciones del mes",   Color.FromArgb(0, 173, 181)),
                     ("Recetas del mes",       "Recetas del mes",      Color.FromArgb(156, 39, 176))
                 })
        {
            metrics.Controls.Add(CreateMetricCard(key, label, color));
        }

        var quickActions = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 10)
        };

        quickActions.Controls.Add(UiHelper.CreatePrimaryButton("Nuevo cliente",    (_, _) => _navigateAction("Pacientes",  true)));
        quickActions.Controls.Add(UiHelper.CreatePrimaryButton("Nueva cita",       (_, _) => _navigateAction("Agenda",     true)));
        quickActions.Controls.Add(UiHelper.CreatePrimaryButton("Nueva atención",   (_, _) => _navigateAction("Atenciones", true)));
        quickActions.Controls.Add(UiHelper.CreatePrimaryButton("Nueva receta",     (_, _) => _navigateAction("Recetas",    true)));
        quickActions.Controls.Add(UiHelper.CreateSecondaryButton("Registrar pago", (_, _) => _navigateAction("Pagos",      true)));
        quickActions.Controls.Add(UiHelper.CreateSecondaryButton("Ver reportes",   (_, _) => _navigateAction("Reportes",   false)));

        UiHelper.ConfigureGrid(_attentionsGrid);
        UiHelper.ConfigureGrid(_prescriptionsGrid);

        var recentRoot = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(0, 4, 0, 0)
        };
        recentRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        recentRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        recentRoot.Controls.Add(CreateRecentPanel("Últimas atenciones",  _attentionsGrid),    0, 0);
        recentRoot.Controls.Add(CreateRecentPanel("Últimas recetas",     _prescriptionsGrid), 1, 0);

        root.Controls.Add(header);
        root.Controls.Add(metrics);
        root.Controls.Add(quickActions);
        root.Controls.Add(recentRoot);

        Controls.Add(root);
    }

    private Panel CreateMetricCard(string key, string title, Color accentColor)
    {
        var valueLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 22F, FontStyle.Bold),
            ForeColor = accentColor,
            Text = "0"
        };

        _metricLabels[key] = valueLabel;

        var accent = new Panel
        {
            BackColor = accentColor,
            Dock = DockStyle.Top,
            Height = 4
        };
        UiHelper.ForcePanelBackground(accent);

        var titleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = Color.FromArgb(100, 110, 130),
            Font = new Font("Segoe UI", 8.5F),
            Padding = new Padding(14, 10, 14, 0),
            Text = title
        };

        var panel = new Panel
        {
            BackColor = Color.White,
            Margin = new Padding(0, 0, 14, 14),
            Width = 180,
            Height = 100
        };

        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(Color.FromArgb(220, 227, 235));
            e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        panel.Controls.Add(accent);
        panel.Controls.Add(titleLabel);
        valueLabel.Top = 36;
        valueLabel.Left = 14;
        panel.Controls.Add(valueLabel);

        return panel;
    }

    private static Panel CreateRecentPanel(string title, Control grid)
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 12, 0) };
        panel.Controls.Add(grid);
        panel.Controls.Add(new Label
        {
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(45, 55, 75),
            Height = 28,
            Text = title
        });
        return panel;
    }

    private void RefreshData()
    {
        var today = DateTime.Today;
        var activeClients = _patientRepository.GetActive();
        var activeProfessionals = _professionalRepository.GetActive();
        var attentions = _attentionRepository.GetAll();
        var prescriptions = _prescriptionRepository.GetAll();

        _metricLabels["Clientes activos"].Text = activeClients.Count.ToString();
        _metricLabels["Profesionales activos"].Text = activeProfessionals.Count.ToString();
        _metricLabels["Atenciones del mes"].Text = attentions
            .Count(x => x.VisitDate.Year == today.Year && x.VisitDate.Month == today.Month).ToString();
        _metricLabels["Recetas del mes"].Text = prescriptions
            .Count(x => x.PrescriptionDate.Year == today.Year && x.PrescriptionDate.Month == today.Month).ToString();

        _attentionsGrid.DataSource = attentions.Take(10).ToList();
        _prescriptionsGrid.DataSource = prescriptions.Take(10).ToList();

        ConfigureAttentionGrid();
        ConfigurePrescriptionGrid();
    }

    private void ConfigureAttentionGrid()
    {
        if (_attentionsGrid.Columns.Count == 0)
            return;

        _attentionsGrid.Columns[nameof(AttentionListItem.Id)].HeaderText = "Id";
        _attentionsGrid.Columns[nameof(AttentionListItem.VisitDate)].HeaderText = "Fecha";
        _attentionsGrid.Columns[nameof(AttentionListItem.PatientName)].HeaderText = "Cliente";
        _attentionsGrid.Columns[nameof(AttentionListItem.ProfessionalName)].HeaderText = "Profesional";
        _attentionsGrid.Columns[nameof(AttentionListItem.ChiefComplaint)].HeaderText = "Motivo";
    }

    private void ConfigurePrescriptionGrid()
    {
        if (_prescriptionsGrid.Columns.Count == 0)
            return;

        _prescriptionsGrid.Columns[nameof(PrescriptionListItem.Id)].HeaderText = "Id";
        _prescriptionsGrid.Columns[nameof(PrescriptionListItem.PrescriptionDate)].HeaderText = "Fecha";
        _prescriptionsGrid.Columns[nameof(PrescriptionListItem.PatientName)].HeaderText = "Cliente";
        _prescriptionsGrid.Columns[nameof(PrescriptionListItem.ProfessionalName)].HeaderText = "Profesional";
        _prescriptionsGrid.Columns[nameof(PrescriptionListItem.Notes)].HeaderText = "Observaciones";
    }
}

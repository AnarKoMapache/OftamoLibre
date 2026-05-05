using OftalmoLibre.Helpers;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class ReportsForm : Form
{
    private readonly ReportService _reportService = new();
    private readonly ExportService _exportService = new();
    private readonly DateTimePicker _fromPicker = new() { Value = DateTime.Today.AddDays(-30) };
    private readonly DateTimePicker _toPicker = new() { Value = DateTime.Today };
    private readonly DataGridView _appointmentsGrid = new() { Dock = DockStyle.Fill };
    private readonly DataGridView _paymentsGrid = new() { Dock = DockStyle.Fill };

    public ReportsForm()
    {
        Text = "Reportes";
        BackColor = Color.White;

        BuildLayout();
        Load += (_, _) => ReloadData();
    }

    private void BuildLayout()
    {
        UiHelper.ConfigureGrid(_appointmentsGrid);
        UiHelper.ConfigureGrid(_paymentsGrid);

        var exportAppointmentsButton = UiHelper.CreateSecondaryButton("Exportar citas", (_, _) => ExportAppointments());
        var exportPaymentsButton     = UiHelper.CreateSecondaryButton("Exportar pagos", (_, _) => ExportPayments());

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Reportes");
        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8) };

        toolbar.Controls.Add(new Label { AutoSize = true, Padding = new Padding(0, 10, 6, 0), Text = "Desde" });
        toolbar.Controls.Add(_fromPicker);
        toolbar.Controls.Add(new Label { AutoSize = true, Padding = new Padding(10, 10, 6, 0), Text = "Hasta" });
        toolbar.Controls.Add(_toPicker);
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Actualizar",     (_, _) => ReloadData()));
        toolbar.Controls.Add(exportAppointmentsButton);
        toolbar.Controls.Add(exportPaymentsButton);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(new TabPage("Citas")  { Controls = { _appointmentsGrid } });
        tabs.TabPages.Add(new TabPage("Pagos")  { Controls = { _paymentsGrid } });

        root.Controls.Add(tabs);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        _appointmentsGrid.DataSource = _reportService.GetAppointmentsReport(_fromPicker.Value.Date, _toPicker.Value.Date);
        _paymentsGrid.DataSource = _reportService.GetPaymentsReport(_fromPicker.Value.Date, _toPicker.Value.Date);
    }

    private void ExportAppointments()
    {
        if (_appointmentsGrid.DataSource is not System.Data.DataTable table)
            return;

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"citas_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _exportService.ExportDataTable(table, dialog.FileName);
        MessageBox.Show("Reporte exportado correctamente.", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportPayments()
    {
        if (_paymentsGrid.DataSource is not System.Data.DataTable table)
            return;

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            FileName = $"pagos_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        _exportService.ExportDataTable(table, dialog.FileName);
        MessageBox.Show("Reporte exportado correctamente.", "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

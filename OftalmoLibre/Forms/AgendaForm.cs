using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Forms;

public sealed class AgendaForm : Form
{
    private readonly User _currentUser;
    private readonly AppointmentRepository _repository = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly DateTimePicker _datePicker = new() { Value = DateTime.Today };
    private readonly ComboBox _statusCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150 };
    private readonly bool _openCreateOnShown;
    private bool _createOpened;

    public AgendaForm(User currentUser, bool openCreateOnShown = false)
    {
        _currentUser = currentUser;
        _openCreateOnShown = openCreateOnShown;

        Text = "Citas";
        BackColor = Color.White;

        BuildLayout();

        Load += (_, _) => ReloadData();
        Shown += (_, _) =>
        {
            if (_openCreateOnShown && !_createOpened)
            {
                _createOpened = true;
                OpenEditor();
            }
        };
    }

    private void BuildLayout()
    {
        UiHelper.ConfigureGrid(_grid);
        _grid.DoubleClick += (_, _) => EditSelected();

        _statusCombo.Items.AddRange(new object[] { "Todos", "No Confirmado", "Confirmado", "Atendido", "Cancelado" });
        _statusCombo.SelectedIndex = 0;

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Agenda de citas");

        var filterPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 8)
        };

        filterPanel.Controls.Add(new Label { AutoSize = true, Padding = new Padding(0, 10, 6, 0), Text = "Fecha" });
        filterPanel.Controls.Add(_datePicker);
        filterPanel.Controls.Add(new Label { AutoSize = true, Padding = new Padding(10, 10, 6, 0), Text = "Estado" });
        filterPanel.Controls.Add(_statusCombo);
        filterPanel.Controls.Add(UiHelper.CreatePrimaryButton("Buscar", (_, _) => ReloadData()));
        filterPanel.Controls.Add(UiHelper.CreatePrimaryButton("Nueva cita", (_, _) => OpenEditor()));
        filterPanel.Controls.Add(UiHelper.CreateSecondaryButton("Editar", (_, _) => EditSelected()));
        filterPanel.Controls.Add(UiHelper.CreateSecondaryButton("Actualizar", (_, _) => ReloadData()));

        root.Controls.Add(_grid);
        root.Controls.Add(filterPanel);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        var status = _statusCombo.SelectedItem?.ToString() switch
        {
            "Todos" => null,
            "No Confirmado" => "Pendiente",
            "Confirmado" => "Confirmada",
            "Atendido" => "Atendida",
            "Cancelado" => "Cancelada",
            var value => value
        };

        _grid.DataSource = _repository.GetAll(_datePicker.Value.Date, status);

        if (_grid.Columns.Count == 0)
        {
            return;
        }

        _grid.Columns[nameof(AppointmentListItem.Id)].HeaderText = "N° Sesión";
        _grid.Columns[nameof(AppointmentListItem.RecordNumber)].HeaderText = "Ficha";
        _grid.Columns[nameof(AppointmentListItem.ScheduledAt)].HeaderText = "Inicio";
        _grid.Columns[nameof(AppointmentListItem.EndAt)].HeaderText = "Fin";
        _grid.Columns[nameof(AppointmentListItem.PatientName)].HeaderText = "Nombre";
        _grid.Columns[nameof(AppointmentListItem.Status)].HeaderText = "Estado";
        _grid.Columns[nameof(AppointmentListItem.PaymentStatus)].HeaderText = "Pago";
        _grid.Columns[nameof(AppointmentListItem.ServiceName)].HeaderText = "Servicio";
        _grid.Columns[nameof(AppointmentListItem.ProfessionalName)].HeaderText = "Profesional";
        _grid.Columns[nameof(AppointmentListItem.Agenda)].HeaderText = "Box";
        _grid.Columns[nameof(AppointmentListItem.Notes)].HeaderText = "Comentario";
        _grid.Columns[nameof(AppointmentListItem.Display)].Visible = false;
    }

    private AppointmentListItem? GetSelected()
    {
        return _grid.CurrentRow?.DataBoundItem as AppointmentListItem;
    }

    private void EditSelected()
    {
        var selected = GetSelected();
        if (selected is null)
        {
            MessageBox.Show("Seleccione una cita.", "Citas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        OpenEditor(selected.Id);
    }

    private void OpenEditor(int? appointmentId = null)
    {
        Appointment? appointment = appointmentId.HasValue ? _repository.GetById(appointmentId.Value) : null;
        using var form = new AppointmentDetailForm(_currentUser, appointment);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            ReloadData();
        }
    }
}

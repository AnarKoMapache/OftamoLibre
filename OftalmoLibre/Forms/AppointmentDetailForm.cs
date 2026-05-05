using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class AppointmentDetailForm : Form
{
    private readonly User _currentUser;
    private readonly AppointmentRepository _appointmentRepository = new();
    private readonly PatientRepository _patientRepository = new();
    private readonly ProfessionalRepository _professionalRepository = new();
    private readonly ServiceRepository _serviceRepository = new();
    private readonly BoxRepository _boxRepository = new();
    private readonly AuditService _auditService = new();
    private readonly Appointment _appointment;
    private Patient? _patient;

    private readonly TextBox _recordNumberTextBox = new();
    private readonly TextBox _fullNameTextBox = new();
    private readonly Label _statusValue = CreateReadOnlyValue();
    private readonly ComboBox _statusCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _paymentCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _phone1TextBox = new();
    private readonly TextBox _phone2TextBox = new();
    private readonly TextBox _emailTextBox = new();

    private readonly DateTimePicker _datePicker = new() { Format = DateTimePickerFormat.Short };
    private readonly DateTimePicker _startPicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm" };
    private readonly DateTimePicker _endPicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm" };
    private readonly ComboBox _serviceCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _professionalCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _agendaCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _notesBox = new() { Multiline = true, Height = 78, ScrollBars = ScrollBars.Vertical };
    private readonly Button _sessionButton = new() { AutoSize = true, Enabled = false, Text = "N° Sesión" };

    public AppointmentDetailForm(User currentUser, Appointment? appointment = null)
    {
        _currentUser = currentUser;
        _appointment = appointment ?? new Appointment { CreatedAt = DateTime.Now, Status = "Pendiente" };
        _patient = _appointment.Id > 0 ? _patientRepository.GetById(_appointment.PatientId) : null;

        Text = _appointment.Id == 0 ? "Nueva cita" : "Editar cita";
        Width = 920;
        Height = 700;
        MinimumSize = new Size(880, 640);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        BuildLayout();
        LoadData();
    }

    private void BuildLayout()
    {
        var topBar = new TableLayoutPanel
        {
            ColumnCount = 3,
            Dock = DockStyle.Top,
            Height = 64,
            Padding = new Padding(16, 12, 16, 8)
        };
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        topBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var titleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = UiHelper.TextPrimary,
            Padding = new Padding(0, 8, 0, 0),
            Text = "Información de la cita"
        };

        ConfigureSessionButton();
        var closeButton = UiHelper.CreateSecondaryButton("Cerrar", (_, _) => Close());
        closeButton.Margin = new Padding(0, 4, 0, 0);

        topBar.Controls.Add(titleLabel, 0, 0);
        topBar.Controls.Add(_sessionButton, 1, 0);
        topBar.Controls.Add(closeButton, 2, 0);

        var divider = new Panel { BackColor = UiHelper.BorderColor, Dock = DockStyle.Top, Height = 1 };

        var root = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 12, 16, 8)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        var left = CreateSidePanel();
        var right = CreateSidePanel();

        AddField(left, "Ficha", _recordNumberTextBox);
        AddField(left, "Nombre", _fullNameTextBox);
        AddField(left, "Estado", _statusValue);
        AddField(left, "Modificar estado", _statusCombo);
        AddField(left, "Pago", _paymentCombo);
        AddField(left, "Teléfono 1", _phone1TextBox);
        AddField(left, "Teléfono 2", _phone2TextBox);
        AddField(left, "Correo", _emailTextBox);

        AddField(right, "Fecha", _datePicker);
        AddField(right, "Inicio", _startPicker);
        AddField(right, "Fin", _endPicker);
        AddField(right, "Servicio", CreatePickerRow(_serviceCombo, (_, _) => OpenServicesCatalog()));
        AddField(right, "Profesional", CreatePickerRow(_professionalCombo, (_, _) => OpenProfessionalsCatalog()));
        AddField(right, "Box", CreatePickerRow(_agendaCombo, (_, _) => OpenBoxesCatalog()));
        AddField(right, "Comentario", _notesBox);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);

        var footer = UiHelper.CreateDialogButtons(
            UiHelper.CreatePrimaryButton("Guardar", (_, _) => Save()),
            UiHelper.CreatePrimaryButton("Pagar", (_, _) => MarkAsPaid()),
            UiHelper.CreateSecondaryButton("Cancelar", (_, _) => Close()));

        _startPicker.ValueChanged += (_, _) => SyncEndTime();
        _serviceCombo.SelectedIndexChanged += (_, _) => SyncEndTimeFromService();
        _statusCombo.SelectedIndexChanged += (_, _) => UpdateStatusLabel();
        _recordNumberTextBox.Leave += (_, _) => TryLoadPatientByRecordNumber();

        Controls.Add(root);
        Controls.Add(footer);
        Controls.Add(divider);
        Controls.Add(topBar);
    }

    private void ConfigureSessionButton()
    {
        _sessionButton.BackColor = Color.White;
        _sessionButton.FlatStyle = FlatStyle.Flat;
        _sessionButton.ForeColor = UiHelper.PrimaryBlue;
        _sessionButton.Height = 36;
        _sessionButton.Margin = new Padding(0, 4, 8, 0);
        _sessionButton.Padding = new Padding(10, 0, 10, 0);
        _sessionButton.FlatAppearance.BorderColor = UiHelper.PrimaryBlue;
        _sessionButton.FlatAppearance.BorderSize = 1;
    }

    private static FlowLayoutPanel CreateSidePanel()
    {
        return new FlowLayoutPanel
        {
            AutoScroll = true,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(0, 0, 12, 0),
            WrapContents = false
        };
    }

    private static void AddField(FlowLayoutPanel panel, string label, Control control)
    {
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F),
            ForeColor = UiHelper.TextSecondary,
            Margin = new Padding(0, 8, 0, 4),
            Text = label
        });

        control.Width = 360;
        control.Margin = new Padding(0, 0, 0, 4);
        panel.Controls.Add(control);
    }

    private static Label CreateReadOnlyValue()
    {
        return new Label
        {
            AutoSize = false,
            BackColor = Color.WhiteSmoke,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 10F),
            ForeColor = UiHelper.TextPrimary,
            Height = 30,
            Padding = new Padding(8, 6, 0, 0)
        };
    }

    private static Panel CreatePickerRow(ComboBox combo, EventHandler onManageClick)
    {
        var panel = new Panel { Height = 32 };
        combo.Dock = DockStyle.Fill;

        var button = new Button
        {
            BackColor = UiHelper.PrimaryBlue,
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Text = "Gestionar",
            Width = 92,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += onManageClick;

        panel.Controls.Add(button);
        panel.Controls.Add(combo);
        return panel;
    }

    private void LoadData()
    {
        ReloadProfessionalChoices();
        ReloadServiceChoices();
        ReloadBoxChoices();

        _statusCombo.Items.AddRange(new object[] { "No Confirmado", "Confirmado", "Atendido", "Cancelado" });
        _paymentCombo.Items.AddRange(new object[] { "No Pagado", "Pagado" });

        if (_appointment.Id == 0)
        {
            var start = DateTime.Today.AddHours(DateTime.Now.Hour + 1);
            _recordNumberTextBox.Text = _patientRepository.GenerateRecordNumber();
            _datePicker.Value = start.Date;
            _startPicker.Value = start;
            _endPicker.Value = start.AddMinutes(15);
            _statusCombo.SelectedItem = "No Confirmado";
            _paymentCombo.SelectedItem = "No Pagado";
            _sessionButton.Text = "N° Sesión";
        }
        else
        {
            _sessionButton.Text = $"N° Sesión {_appointment.Id}";
            _recordNumberTextBox.Text = _patient?.RecordNumber ?? string.Empty;
            _fullNameTextBox.Text = _patient?.FullName ?? string.Empty;
            _phone1TextBox.Text = _patient?.Phone1 ?? string.Empty;
            _phone2TextBox.Text = _patient?.Phone2 ?? string.Empty;
            _emailTextBox.Text = _patient?.Email ?? string.Empty;
            _professionalCombo.SelectedValue = _appointment.ProfessionalId;
            _serviceCombo.SelectedValue = _appointment.ServiceId;
            _datePicker.Value = _appointment.ScheduledAt.Date;
            _startPicker.Value = _appointment.ScheduledAt;
            _endPicker.Value = _appointment.EndAt ?? _appointment.ScheduledAt.AddMinutes(15);
            _statusCombo.SelectedItem = ToDisplayStatus(_appointment.Status);
            _paymentCombo.SelectedItem = string.IsNullOrWhiteSpace(_appointment.PaymentStatus) ? "No Pagado" : _appointment.PaymentStatus;
            SelectAgenda(_appointment.Agenda);
            _notesBox.Text = _appointment.Notes ?? string.Empty;
        }

        if (_professionalCombo.SelectedIndex < 0 && _professionalCombo.Items.Count > 0)
        {
            _professionalCombo.SelectedIndex = 0;
        }

        if (_serviceCombo.SelectedIndex < 0 && _serviceCombo.Items.Count > 0)
        {
            _serviceCombo.SelectedIndex = 0;
        }

        if (_agendaCombo.SelectedIndex < 0 && _agendaCombo.Items.Count > 0)
        {
            _agendaCombo.SelectedIndex = 0;
        }

        UpdateStatusLabel();
    }

    private void ReloadProfessionalChoices()
    {
        var selected = _professionalCombo.SelectedValue as int?;
        _professionalCombo.DataSource = _professionalRepository.GetActive();
        _professionalCombo.DisplayMember = nameof(Professional.FullName);
        _professionalCombo.ValueMember = nameof(Professional.Id);

        if (selected.HasValue)
        {
            _professionalCombo.SelectedValue = selected.Value;
        }
    }

    private void ReloadServiceChoices()
    {
        var selected = _serviceCombo.SelectedValue as int?;
        _serviceCombo.DataSource = _serviceRepository.GetActive();
        _serviceCombo.DisplayMember = nameof(OphthalmologyService.Name);
        _serviceCombo.ValueMember = nameof(OphthalmologyService.Id);

        if (selected.HasValue)
        {
            _serviceCombo.SelectedValue = selected.Value;
        }
    }

    private void ReloadBoxChoices()
    {
        var selected = _agendaCombo.SelectedItem?.ToString() ?? _appointment.Agenda ?? "BOX CONDELL";
        var boxes = _boxRepository.GetActiveNames();
        if (!boxes.Contains(selected, StringComparer.OrdinalIgnoreCase))
        {
            boxes.Insert(0, selected);
        }

        _agendaCombo.DataSource = boxes;
        SelectAgenda(selected);
    }

    private void SelectAgenda(string? agenda)
    {
        if (string.IsNullOrWhiteSpace(agenda))
        {
            return;
        }

        for (var i = 0; i < _agendaCombo.Items.Count; i++)
        {
            if (string.Equals(_agendaCombo.Items[i]?.ToString(), agenda, StringComparison.OrdinalIgnoreCase))
            {
                _agendaCombo.SelectedIndex = i;
                return;
            }
        }
    }

    private void OpenServicesCatalog()
    {
        using var form = new ServicesForm(_currentUser);
        form.ShowDialog(this);
        ReloadServiceChoices();
    }

    private void OpenProfessionalsCatalog()
    {
        using var form = new ProfessionalsForm(_currentUser);
        form.ShowDialog(this);
        ReloadProfessionalChoices();
    }

    private void OpenBoxesCatalog()
    {
        using var form = new BoxesForm(_currentUser);
        form.ShowDialog(this);
        ReloadBoxChoices();
    }

    private void TryLoadPatientByRecordNumber()
    {
        if (string.IsNullOrWhiteSpace(_recordNumberTextBox.Text))
        {
            return;
        }

        var existing = _patientRepository.GetByRecordNumber(_recordNumberTextBox.Text);
        if (existing is null)
        {
            return;
        }

        _patient = existing;
        _fullNameTextBox.Text = existing.FullName;
        _phone1TextBox.Text = existing.Phone1 ?? string.Empty;
        _phone2TextBox.Text = existing.Phone2 ?? string.Empty;
        _emailTextBox.Text = existing.Email ?? string.Empty;
    }

    private void SyncEndTime()
    {
        if (_endPicker.Value <= _startPicker.Value)
        {
            _endPicker.Value = _startPicker.Value.AddMinutes(15);
        }
    }

    private void SyncEndTimeFromService()
    {
        if (_serviceCombo.SelectedItem is not OphthalmologyService service)
        {
            return;
        }

        _endPicker.Value = _startPicker.Value.AddMinutes(service.DurationMinutes <= 0 ? 15 : service.DurationMinutes);
    }

    private void UpdateStatusLabel()
    {
        _statusValue.Text = _statusCombo.SelectedItem?.ToString() ?? "No Confirmado";
    }

    private static string ToDisplayStatus(string status)
    {
        return status switch
        {
            "Pendiente" => "No Confirmado",
            "Confirmada" => "Confirmado",
            "Atendida" => "Atendido",
            "Cancelada" => "Cancelado",
            _ => "No Confirmado"
        };
    }

    private static string ToStoredStatus(string displayStatus)
    {
        return displayStatus switch
        {
            "No Confirmado" => "Pendiente",
            "Confirmado" => "Confirmada",
            "Atendido" => "Atendida",
            "Cancelado" => "Cancelada",
            _ => "Pendiente"
        };
    }

    private void MarkAsPaid()
    {
        _paymentCombo.SelectedItem = "Pagado";
        Save();
    }

    private Patient SavePatient()
    {
        var existingByRecord = _patientRepository.GetByRecordNumber(_recordNumberTextBox.Text);
        if (existingByRecord is not null && (_patient is null || existingByRecord.Id != _patient.Id))
        {
            _patient = existingByRecord;
        }

        _patient ??= new Patient();
        _patient.RecordNumber = _recordNumberTextBox.Text.Trim();
        _patient.FullName = _fullNameTextBox.Text.Trim();
        _patient.Phone1 = _phone1TextBox.Text.Trim();
        _patient.Phone2 = _phone2TextBox.Text.Trim();
        _patient.Email = _emailTextBox.Text.Trim();
        _patient.IsActive = true;

        _patientRepository.Save(_patient);
        return _patient;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_recordNumberTextBox.Text) ||
            string.IsNullOrWhiteSpace(_fullNameTextBox.Text) ||
            _professionalCombo.SelectedValue is null ||
            _serviceCombo.SelectedValue is null)
        {
            MessageBox.Show("Debe completar ficha, nombre, profesional y servicio.",
                "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidationHelper.IsValidEmail(_emailTextBox.Text))
        {
            MessageBox.Show("El correo no tiene un formato válido.",
                "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var isNew = _appointment.Id == 0;
        var patient = SavePatient();

        _appointment.PatientId = patient.Id;
        _appointment.ProfessionalId = Convert.ToInt32(_professionalCombo.SelectedValue);
        _appointment.ServiceId = Convert.ToInt32(_serviceCombo.SelectedValue);
        _appointment.ScheduledAt = _datePicker.Value.Date + _startPicker.Value.TimeOfDay;
        _appointment.EndAt = _datePicker.Value.Date + _endPicker.Value.TimeOfDay;
        _appointment.Status = ToStoredStatus(_statusCombo.SelectedItem?.ToString() ?? "No Confirmado");
        _appointment.PaymentStatus = _paymentCombo.SelectedItem?.ToString() ?? "No Pagado";
        _appointment.Agenda = _agendaCombo.SelectedItem?.ToString() ?? "BOX CONDELL";
        _appointment.Notes = _notesBox.Text.Trim();

        _appointmentRepository.Save(_appointment);
        _sessionButton.Text = $"N° Sesión {_appointment.Id}";
        _auditService.Log(_currentUser.Id, isNew ? "Crear" : "Actualizar",
            "Cita", _appointment.Id.ToString(), _appointment.Status);

        DialogResult = DialogResult.OK;
        Close();
    }
}

using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class PatientEditorForm : Form
{
    private readonly User _currentUser;
    private readonly PatientRepository _repository = new();
    private readonly AuditService _auditService = new();
    private readonly Patient _patient;

    private readonly TextBox _recordNumberBox  = new() { ReadOnly = true, BackColor = Color.FromArgb(245, 247, 250) };
    private readonly TextBox _fullNameBox       = new();
    private readonly TextBox _documentBox       = new();
    private readonly DateTimePicker _birthPicker= new() { Format = DateTimePickerFormat.Short, ShowCheckBox = true };
    private readonly Label   _ageLabel          = new() { AutoSize = true };
    private readonly TextBox _phone1Box         = new();
    private readonly TextBox _phone2Box         = new();
    private readonly TextBox _emailBox          = new();
    private readonly TextBox _addressBox        = new();
    private readonly CheckBox _glassesCheck     = new() { Text = "Lentes",   AutoSize = true };
    private readonly CheckBox _lensesCheck      = new() { Text = "Contacto", AutoSize = true };
    private readonly CheckBox _activeCheck      = new() { Text = "Activo",   AutoSize = true, Checked = true };
    private readonly TextBox _notesBox          = new() { Multiline = true, Height = 70, ScrollBars = ScrollBars.Vertical };

    public PatientEditorForm(User currentUser, Patient? patient = null)
    {
        _currentUser = currentUser;
        _patient = patient ?? new Patient { CreatedAt = DateTime.Now, IsActive = true };

        Text = _patient.Id == 0 ? "Nuevo cliente" : "Editar cliente";
        Width  = 700;
        Height = 490;
        MinimumSize = new Size(640, 460);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        BuildLayout();
        LoadData();
    }

    private void BuildLayout()
    {
        var title = new Label
        {
            AutoSize  = false,
            Dock      = DockStyle.Top,
            Font      = new Font("Segoe UI", 13F, FontStyle.Bold),
            ForeColor = UiHelper.TextPrimary,
            Height    = 50,
            Padding   = new Padding(18, 14, 0, 0),
            Text      = "Información del cliente"
        };
        var divider = new Panel { BackColor = UiHelper.BorderColor, Dock = DockStyle.Top, Height = 1 };

        var body = UiHelper.Create2ColLayout();

        // Birth date + age inline
        var birthRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        birthRow.Controls.Add(_birthPicker);
        birthRow.Controls.Add(new Label { AutoSize = true, Padding = new Padding(10, 7, 4, 0), Text = "Edad:" });
        birthRow.Controls.Add(_ageLabel);
        _birthPicker.ValueChanged += (_, _) => UpdateAge();
        _documentBox.Leave += (_, _) => _documentBox.Text = DocumentNumberHelper.Normalize(_documentBox.Text) ?? string.Empty;

        // Visual aid checkboxes
        var visionRow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        visionRow.Controls.Add(_glassesCheck);
        visionRow.Controls.Add(new Label { AutoSize = true, Padding = new Padding(10, 4, 0, 0) });
        visionRow.Controls.Add(_lensesCheck);

        int r = 0;
        UiHelper.Add2Row(body, r++, "Ficha",          _recordNumberBox,
                                    "Teléfono 1",      _phone1Box);
        UiHelper.Add2Row(body, r++, "Nombre completo", _fullNameBox,
                                    "Teléfono 2",      _phone2Box);
        UiHelper.Add2Row(body, r++, "RUT / DNI",       _documentBox,
                                    "Correo",          _emailBox);
        UiHelper.Add2Row(body, r++, "F. Nacimiento",   birthRow,
                                    "Dirección",       _addressBox);
        UiHelper.Add2Row(body, r++, "Apoyo visual",    visionRow,
                                    "Estado",          _activeCheck);
        UiHelper.Add2RowFull(body, r++, "Observaciones", _notesBox);

        var footer = UiHelper.CreateDialogButtons(
            UiHelper.CreatePrimaryButton("Guardar",    (_, _) => Save()),
            UiHelper.CreateSecondaryButton("Cancelar", (_, _) => Close()));

        Controls.Add(body);
        Controls.Add(footer);
        Controls.Add(divider);
        Controls.Add(title);
    }

    private void LoadData()
    {
        _recordNumberBox.Text  = _patient.Id == 0 ? _repository.GenerateRecordNumber() : _patient.RecordNumber;
        _fullNameBox.Text      = _patient.FullName;
        _documentBox.Text      = _patient.DocumentNumber ?? "";
        _birthPicker.Checked   = _patient.BirthDate.HasValue;
        _birthPicker.Value     = _patient.BirthDate ?? DateTime.Today;
        _phone1Box.Text        = _patient.Phone1 ?? "";
        _phone2Box.Text        = _patient.Phone2 ?? "";
        _emailBox.Text         = _patient.Email ?? "";
        _addressBox.Text       = _patient.Address ?? "";
        _glassesCheck.Checked  = _patient.UsesGlasses;
        _lensesCheck.Checked   = _patient.ContactLenses;
        _activeCheck.Checked   = _patient.IsActive;
        _notesBox.Text         = _patient.GeneralNotes ?? "";
        UpdateAge();
    }

    private void UpdateAge()
    {
        var age = _birthPicker.Checked ? DateHelper.CalculateAge(_birthPicker.Value.Date) : 0;
        _ageLabel.Text = $"{age} años";
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_fullNameBox.Text))
        {
            MessageBox.Show("El nombre del cliente es obligatorio.", "Validación",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(_emailBox.Text) && !ValidationHelper.IsValidEmail(_emailBox.Text))
        {
            MessageBox.Show("El correo electrónico no tiene un formato válido.", "Validación",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_repository.ExistsDocument(_documentBox.Text, _patient.Id == 0 ? null : _patient.Id))
        {
            MessageBox.Show("El RUT / DNI ya está registrado en otro cliente.", "Validación",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var isNew = _patient.Id == 0;
        _patient.RecordNumber    = _recordNumberBox.Text.Trim();
        _patient.FullName        = _fullNameBox.Text.Trim();
        _patient.DocumentNumber  = DocumentNumberHelper.Normalize(_documentBox.Text);
        _patient.BirthDate       = _birthPicker.Checked ? _birthPicker.Value.Date : null;
        _patient.Phone1          = _phone1Box.Text.Trim().NullIfEmpty();
        _patient.Phone2          = _phone2Box.Text.Trim().NullIfEmpty();
        _patient.Email           = _emailBox.Text.Trim().NullIfEmpty();
        _patient.Address         = _addressBox.Text.Trim().NullIfEmpty();
        _patient.UsesGlasses     = _glassesCheck.Checked;
        _patient.ContactLenses   = _lensesCheck.Checked;
        _patient.IsActive        = _activeCheck.Checked;
        _patient.GeneralNotes    = _notesBox.Text.Trim().NullIfEmpty();

        _repository.Save(_patient);
        _auditService.Log(_currentUser.Id, isNew ? "Crear" : "Actualizar",
            "Cliente", _patient.Id.ToString(), _patient.FullName);

        DialogResult = DialogResult.OK;
        Close();
    }
}

static class StringExtensions2
{
    public static string? NullIfEmpty(this string s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}

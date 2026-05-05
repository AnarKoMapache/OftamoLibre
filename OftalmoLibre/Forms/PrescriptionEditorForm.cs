using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class PrescriptionEditorForm : Form
{
    private readonly User _currentUser;
    private readonly PrescriptionRepository _repository = new();
    private readonly PatientRepository _patientRepository = new();
    private readonly ProfessionalRepository _professionalRepository = new();
    private readonly AuditService _auditService = new();
    private readonly OpticalPrescription _prescription;
    private Patient? _patient;

    private readonly TextBox _recordNumberTextBox = new() { ReadOnly = true, BackColor = Color.FromArgb(245, 247, 250) };
    private readonly TextBox _documentNumberTextBox = new();
    private readonly TextBox _patientNameTextBox = new();
    private readonly ComboBox _professionalCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _datePicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
    private readonly TextBox _sphereRightTextBox = new();
    private readonly TextBox _cylinderRightTextBox = new();
    private readonly TextBox _axisRightTextBox = new();
    private readonly TextBox _sphereLeftTextBox = new();
    private readonly TextBox _cylinderLeftTextBox = new();
    private readonly TextBox _axisLeftTextBox = new();
    private readonly TextBox _addPowerTextBox = new();
    private readonly TextBox _pdTextBox = new();
    private readonly TextBox _notesTextBox = new() { Multiline = true, Height = 80, ScrollBars = ScrollBars.Vertical };

    public PrescriptionEditorForm(User currentUser, OpticalPrescription? prescription = null)
    {
        _currentUser = currentUser;
        _prescription = prescription ?? new OpticalPrescription { PrescriptionDate = DateTime.Now };
        _patient = _prescription.Id > 0 ? _patientRepository.GetById(_prescription.PatientId) : null;

        Text = _prescription.Id == 0 ? "Nueva receta oftalmológica" : "Editar receta oftalmológica";
        Width = 780;
        Height = 640;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadData();
    }

    private void BuildLayout()
    {
        var layout = UiHelper.CreateEditorLayout(11);
        UiHelper.AddLabeledControl(layout, "Ficha", _recordNumberTextBox, 0);
        UiHelper.AddLabeledControl(layout, "RUT", _documentNumberTextBox, 1);
        UiHelper.AddLabeledControl(layout, "Nombre del cliente", _patientNameTextBox, 2);
        UiHelper.AddLabeledControl(layout, "Profesional", CreateComboRow(_professionalCombo, (_, _) => OpenProfessionalsCatalog()), 3);
        UiHelper.AddLabeledControl(layout, "Fecha", _datePicker, 4);
        UiHelper.AddLabeledControl(layout, "OD Esfera / Cil / Eje", CreateTriplet(_sphereRightTextBox, _cylinderRightTextBox, _axisRightTextBox), 5);
        UiHelper.AddLabeledControl(layout, "OI Esfera / Cil / Eje", CreateTriplet(_sphereLeftTextBox, _cylinderLeftTextBox, _axisLeftTextBox), 6);
        UiHelper.AddLabeledControl(layout, "Adición", _addPowerTextBox, 7);
        UiHelper.AddLabeledControl(layout, "Distancia interpupilar", _pdTextBox, 8);
        UiHelper.AddLabeledControl(layout, "Observaciones", _notesTextBox, 9);
        UiHelper.AddLabeledControl(layout, string.Empty, new Label { AutoSize = true, Text = string.Empty }, 10);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 56,
            Padding = new Padding(12)
        };
        buttons.Controls.Add(UiHelper.CreatePrimaryButton("Guardar", (_, _) => Save()));
        buttons.Controls.Add(UiHelper.CreateSecondaryButton("Cancelar", (_, _) => Close()));

        _documentNumberTextBox.Leave += (_, _) => TryLoadPatientByRut();
        _documentNumberTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                TryLoadPatientByRut();
            }
        };

        Controls.Add(layout);
        Controls.Add(buttons);
    }

    private static Panel CreateComboRow(ComboBox combo, EventHandler onManageClick)
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

    private static FlowLayoutPanel CreateTriplet(params TextBox[] controls)
    {
        var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top };
        foreach (var control in controls)
        {
            control.Width = 90;
            panel.Controls.Add(control);
        }

        return panel;
    }

    private void LoadData()
    {
        ReloadProfessionalChoices();

        if (_prescription.Id == 0)
        {
            _recordNumberTextBox.Text = _patientRepository.GenerateRecordNumber();
            return;
        }

        _recordNumberTextBox.Text = _patient?.RecordNumber ?? string.Empty;
        _documentNumberTextBox.Text = DocumentNumberHelper.Normalize(_patient?.DocumentNumber) ?? string.Empty;
        _patientNameTextBox.Text = _patient?.FullName ?? string.Empty;
        _professionalCombo.SelectedValue = _prescription.ProfessionalId;
        _datePicker.Value = _prescription.PrescriptionDate;
        _sphereRightTextBox.Text = _prescription.SphereRight;
        _cylinderRightTextBox.Text = _prescription.CylinderRight;
        _axisRightTextBox.Text = _prescription.AxisRight;
        _sphereLeftTextBox.Text = _prescription.SphereLeft;
        _cylinderLeftTextBox.Text = _prescription.CylinderLeft;
        _axisLeftTextBox.Text = _prescription.AxisLeft;
        _addPowerTextBox.Text = _prescription.AddPower;
        _pdTextBox.Text = _prescription.PupillaryDistance;
        _notesTextBox.Text = _prescription.Notes;
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
        else if (_professionalCombo.Items.Count > 0)
        {
            _professionalCombo.SelectedIndex = 0;
        }
    }

    private void OpenProfessionalsCatalog()
    {
        using var form = new ProfessionalsForm(_currentUser);
        form.ShowDialog(this);
        ReloadProfessionalChoices();
    }

    private void TryLoadPatientByRut()
    {
        _documentNumberTextBox.Text = DocumentNumberHelper.Normalize(_documentNumberTextBox.Text) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(_documentNumberTextBox.Text))
        {
            return;
        }

        var existing = _patientRepository.GetByDocumentNumber(_documentNumberTextBox.Text);
        if (existing is null)
        {
            return;
        }

        _patient = existing;
        _recordNumberTextBox.Text = existing.RecordNumber;
        _patientNameTextBox.Text = existing.FullName;
    }

    private Patient SavePatient()
    {
        var existing = _patientRepository.GetByDocumentNumber(_documentNumberTextBox.Text);
        if (existing is not null && (_patient is null || existing.Id != _patient.Id))
        {
            _patient = existing;
        }

        _patient ??= new Patient();
        _patient.RecordNumber = string.IsNullOrWhiteSpace(_patient.RecordNumber) ? _patientRepository.GenerateRecordNumber() : _patient.RecordNumber;
        _patient.DocumentNumber = DocumentNumberHelper.Normalize(_documentNumberTextBox.Text);
        _patient.FullName = _patientNameTextBox.Text.Trim();
        _patient.IsActive = true;

        _patientRepository.Save(_patient);
        _documentNumberTextBox.Text = _patient.DocumentNumber ?? string.Empty;
        _recordNumberTextBox.Text = _patient.RecordNumber;
        return _patient;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_documentNumberTextBox.Text) ||
            string.IsNullOrWhiteSpace(_patientNameTextBox.Text) ||
            _professionalCombo.SelectedValue is null)
        {
            MessageBox.Show("Debe completar RUT, nombre del cliente y profesional.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var isNew = _prescription.Id == 0;
        var patient = SavePatient();

        _prescription.PatientId = patient.Id;
        _prescription.ProfessionalId = Convert.ToInt32(_professionalCombo.SelectedValue);
        _prescription.PrescriptionDate = _datePicker.Value;
        _prescription.SphereRight = _sphereRightTextBox.Text.Trim();
        _prescription.CylinderRight = _cylinderRightTextBox.Text.Trim();
        _prescription.AxisRight = _axisRightTextBox.Text.Trim();
        _prescription.SphereLeft = _sphereLeftTextBox.Text.Trim();
        _prescription.CylinderLeft = _cylinderLeftTextBox.Text.Trim();
        _prescription.AxisLeft = _axisLeftTextBox.Text.Trim();
        _prescription.AddPower = _addPowerTextBox.Text.Trim();
        _prescription.PupillaryDistance = _pdTextBox.Text.Trim();
        _prescription.Notes = _notesTextBox.Text.Trim();

        _repository.Save(_prescription);
        _auditService.Log(_currentUser.Id, isNew ? "Crear" : "Actualizar", "Receta", _prescription.Id.ToString(), _prescription.Notes);

        DialogResult = DialogResult.OK;
        Close();
    }
}

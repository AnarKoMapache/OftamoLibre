using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class AttentionEditorForm : Form
{
    private readonly User _currentUser;
    private readonly AttentionRepository _repository = new();
    private readonly PatientRepository _patientRepository = new();
    private readonly ProfessionalRepository _professionalRepository = new();
    private readonly AuditService _auditService = new();
    private readonly Attention _attention;

    private readonly ComboBox _patientCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _professionalCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _visitDatePicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
    private readonly TextBox _chiefComplaintTextBox = new() { Multiline = true, Height = 60 };
    private readonly TextBox _clinicalNotesTextBox = new() { Multiline = true, Height = 110 };
    private readonly TextBox _planTextBox = new() { Multiline = true, Height = 80 };
    private readonly TextBox _vaRightTextBox = new();
    private readonly TextBox _vaLeftTextBox = new();

    public AttentionEditorForm(User currentUser, Attention? attention = null)
    {
        _currentUser = currentUser;
        _attention = attention ?? new Attention { VisitDate = DateTime.Now };

        Text = _attention.Id == 0 ? "Nueva atención" : "Editar atención";
        Width = 760;
        Height = 620;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadData();
    }

    private void BuildLayout()
    {
        var layout = UiHelper.CreateEditorLayout(8);
        UiHelper.AddLabeledControl(layout, "Cliente", _patientCombo, 0);
        UiHelper.AddLabeledControl(layout, "Profesional", _professionalCombo, 1);
        UiHelper.AddLabeledControl(layout, "Fecha de atención", _visitDatePicker, 2);
        UiHelper.AddLabeledControl(layout, "Motivo de consulta", _chiefComplaintTextBox, 3);
        UiHelper.AddLabeledControl(layout, "Notas de atención", _clinicalNotesTextBox, 4);
        UiHelper.AddLabeledControl(layout, "Indicaciones / plan", _planTextBox, 5);
        UiHelper.AddLabeledControl(layout, "Agudeza visual OD", _vaRightTextBox, 6);
        UiHelper.AddLabeledControl(layout, "Agudeza visual OI", _vaLeftTextBox, 7);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 56, Padding = new Padding(12) };
        var saveButton = UiHelper.CreatePrimaryButton("Guardar", (_, _) => Save());
        var cancelButton = new Button { AutoSize = true, Text = "Cancelar" };
        cancelButton.Click += (_, _) => Close();
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);

        Controls.Add(layout);
        Controls.Add(buttons);
    }

    private void LoadData()
    {
        _patientCombo.DataSource = _patientRepository.GetActive();
        _patientCombo.DisplayMember = nameof(Patient.FullName);
        _patientCombo.ValueMember = nameof(Patient.Id);

        _professionalCombo.DataSource = _professionalRepository.GetActive();
        _professionalCombo.DisplayMember = nameof(Professional.FullName);
        _professionalCombo.ValueMember = nameof(Professional.Id);

        if (_attention.Id > 0)
        {
            _patientCombo.SelectedValue = _attention.PatientId;
            _professionalCombo.SelectedValue = _attention.ProfessionalId;
            _visitDatePicker.Value = _attention.VisitDate;
            _chiefComplaintTextBox.Text = _attention.ChiefComplaint;
            _clinicalNotesTextBox.Text = _attention.ClinicalNotes;
            _planTextBox.Text = _attention.Plan;
            _vaRightTextBox.Text = _attention.VisualAcuityRight;
            _vaLeftTextBox.Text = _attention.VisualAcuityLeft;
        }
    }

    private void Save()
    {
        if (_patientCombo.SelectedValue is null || _professionalCombo.SelectedValue is null)
        {
            MessageBox.Show("Debe seleccionar cliente y profesional.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var isNew = _attention.Id == 0;
        _attention.PatientId = Convert.ToInt32(_patientCombo.SelectedValue);
        _attention.ProfessionalId = Convert.ToInt32(_professionalCombo.SelectedValue);
        _attention.VisitDate = _visitDatePicker.Value;
        _attention.ChiefComplaint = _chiefComplaintTextBox.Text.Trim();
        _attention.ClinicalNotes = _clinicalNotesTextBox.Text.Trim();
        _attention.Plan = _planTextBox.Text.Trim();
        _attention.VisualAcuityRight = _vaRightTextBox.Text.Trim();
        _attention.VisualAcuityLeft = _vaLeftTextBox.Text.Trim();

        _repository.Save(_attention);
        _auditService.Log(_currentUser.Id, isNew ? "Crear" : "Actualizar", "Atención", _attention.Id.ToString(), _attention.ChiefComplaint);

        DialogResult = DialogResult.OK;
        Close();
    }
}

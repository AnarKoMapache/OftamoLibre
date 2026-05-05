using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class ExamEditorForm : Form
{
    private readonly User _currentUser;
    private readonly ExamRepository _repository = new();
    private readonly PatientRepository _patientRepository = new();
    private readonly ProfessionalRepository _professionalRepository = new();
    private readonly AttentionRepository _attentionRepository = new();
    private readonly AuditService _auditService = new();
    private readonly EyeExam _exam;

    private readonly ComboBox _patientCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _professionalCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _attentionCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _datePicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
    private readonly TextBox _examTypeTextBox = new();
    private readonly TextBox _resultTextBox = new() { Multiline = true, Height = 90 };
    private readonly TextBox _notesTextBox = new() { Multiline = true, Height = 90 };

    public ExamEditorForm(User currentUser, EyeExam? exam = null)
    {
        _currentUser = currentUser;
        _exam = exam ?? new EyeExam { ExamDate = DateTime.Now };

        Text = _exam.Id == 0 ? "Nuevo examen" : "Editar examen";
        Width = 720;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadData();
    }

    private void BuildLayout()
    {
        var layout = UiHelper.CreateEditorLayout(7);
        UiHelper.AddLabeledControl(layout, "Paciente", _patientCombo, 0);
        UiHelper.AddLabeledControl(layout, "Profesional", _professionalCombo, 1);
        UiHelper.AddLabeledControl(layout, "Atención asociada", _attentionCombo, 2);
        UiHelper.AddLabeledControl(layout, "Fecha", _datePicker, 3);
        UiHelper.AddLabeledControl(layout, "Tipo de examen", _examTypeTextBox, 4);
        UiHelper.AddLabeledControl(layout, "Resultado", _resultTextBox, 5);
        UiHelper.AddLabeledControl(layout, "Observaciones", _notesTextBox, 6);

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
        _patientCombo.SelectedIndexChanged += (_, _) => LoadAttentions();

        _professionalCombo.DataSource = _professionalRepository.GetActive();
        _professionalCombo.DisplayMember = nameof(Professional.FullName);
        _professionalCombo.ValueMember = nameof(Professional.Id);

        if (_exam.Id > 0)
        {
            _patientCombo.SelectedValue = _exam.PatientId;
            _professionalCombo.SelectedValue = _exam.ProfessionalId;
        }

        LoadAttentions();

        if (_exam.Id > 0)
        {
            _attentionCombo.SelectedValue = _exam.AttentionId;
            _datePicker.Value = _exam.ExamDate;
            _examTypeTextBox.Text = _exam.ExamType;
            _resultTextBox.Text = _exam.ResultSummary;
            _notesTextBox.Text = _exam.Notes;
        }
    }

    private void LoadAttentions()
    {
        var items = new List<LookupItem> { new() { Id = null, Text = "(Sin atención asociada)" } };
        if (_patientCombo.SelectedValue is int patientId)
        {
            items.AddRange(_attentionRepository.GetByPatientForLookup(patientId).Select(x => new LookupItem
            {
                Id = x.Id,
                Text = $"{x.VisitDate:g} - {x.ChiefComplaint}"
            }));
        }

        _attentionCombo.DataSource = items;
        _attentionCombo.DisplayMember = nameof(LookupItem.Text);
        _attentionCombo.ValueMember = nameof(LookupItem.Id);
    }

    private void Save()
    {
        if (_patientCombo.SelectedValue is null || _professionalCombo.SelectedValue is null || string.IsNullOrWhiteSpace(_examTypeTextBox.Text))
        {
            MessageBox.Show("Paciente, profesional y tipo de examen son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var isNew = _exam.Id == 0;
        _exam.PatientId = Convert.ToInt32(_patientCombo.SelectedValue);
        _exam.ProfessionalId = Convert.ToInt32(_professionalCombo.SelectedValue);
        _exam.AttentionId = _attentionCombo.SelectedValue as int?;
        _exam.ExamDate = _datePicker.Value;
        _exam.ExamType = _examTypeTextBox.Text.Trim();
        _exam.ResultSummary = _resultTextBox.Text.Trim();
        _exam.Notes = _notesTextBox.Text.Trim();

        _repository.Save(_exam);
        _auditService.Log(_currentUser.Id, isNew ? "Crear" : "Actualizar", "Examen", _exam.Id.ToString(), _exam.ExamType);

        DialogResult = DialogResult.OK;
        Close();
    }
}

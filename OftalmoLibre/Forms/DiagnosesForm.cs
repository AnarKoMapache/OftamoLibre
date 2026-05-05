using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class DiagnosesForm : Form
{
    private readonly User _currentUser;
    private readonly DiagnosisRepository _repository = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };

    public DiagnosesForm(User currentUser)
    {
        _currentUser = currentUser;
        Text = "Diagnósticos";
        BackColor = Color.White;

        BuildLayout();
        Load += (_, _) => ReloadData();
    }

    private void BuildLayout()
    {
        UiHelper.ConfigureGrid(_grid);
        _grid.DoubleClick += (_, _) => EditSelected();

        var editButton    = UiHelper.CreateSecondaryButton("Editar",    (_, _) => EditSelected());
        var refreshButton = UiHelper.CreateSecondaryButton("Actualizar",(_, _) => ReloadData());

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Diagnósticos");
        var toolbar = UiHelper.CreateToolbar(
            UiHelper.CreatePrimaryButton("Nuevo diagnóstico", (_, _) => OpenEditor()),
            editButton,
            refreshButton);

        root.Controls.Add(_grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        _grid.DataSource = _repository.GetAll();
        if (_grid.Columns.Count > 0)
        {
            _grid.Columns[nameof(DiagnosisListItem.Id)].HeaderText = "Id";
            _grid.Columns[nameof(DiagnosisListItem.DiagnosisDate)].HeaderText = "Fecha";
            _grid.Columns[nameof(DiagnosisListItem.PatientName)].HeaderText = "Paciente";
            _grid.Columns[nameof(DiagnosisListItem.ProfessionalName)].HeaderText = "Profesional";
            _grid.Columns[nameof(DiagnosisListItem.Description)].HeaderText = "Diagnóstico";
        }
    }

    private DiagnosisListItem? GetSelected() => _grid.CurrentRow?.DataBoundItem as DiagnosisListItem;

    private void EditSelected()
    {
        var item = GetSelected();
        if (item is null)
        {
            MessageBox.Show("Seleccione un diagnóstico.", "Diagnósticos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(item.Id);
    }

    private void OpenEditor(int? diagnosisId = null)
    {
        var diagnosis = diagnosisId.HasValue ? _repository.GetById(diagnosisId.Value) : null;
        using var dialog = new DiagnosisEditorDialog(_currentUser, diagnosis);
        if (dialog.ShowDialog(this) == DialogResult.OK)
            ReloadData();
    }

    private sealed class DiagnosisEditorDialog : Form
    {
        private readonly User _currentUser;
        private readonly DiagnosisRepository _repository = new();
        private readonly PatientRepository _patientRepository = new();
        private readonly ProfessionalRepository _professionalRepository = new();
        private readonly AttentionRepository _attentionRepository = new();
        private readonly AuditService _auditService = new();
        private readonly Diagnosis _diagnosis;

        private readonly ComboBox _patientCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _professionalCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly ComboBox _attentionCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly DateTimePicker _datePicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
        private readonly TextBox _codeTextBox = new();
        private readonly TextBox _descriptionTextBox = new() { Multiline = true, Height = 80 };
        private readonly TextBox _notesTextBox = new() { Multiline = true, Height = 80 };

        public DiagnosisEditorDialog(User currentUser, Diagnosis? diagnosis)
        {
            _currentUser = currentUser;
            _diagnosis = diagnosis ?? new Diagnosis { DiagnosisDate = DateTime.Now };

            Text = _diagnosis.Id == 0 ? "Nuevo diagnóstico" : "Editar diagnóstico";
            Width = 720;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;

            var layout = UiHelper.CreateEditorLayout(7);
            UiHelper.AddLabeledControl(layout, "Paciente",          _patientCombo,      0);
            UiHelper.AddLabeledControl(layout, "Profesional",       _professionalCombo, 1);
            UiHelper.AddLabeledControl(layout, "Atención asociada", _attentionCombo,    2);
            UiHelper.AddLabeledControl(layout, "Fecha",             _datePicker,        3);
            UiHelper.AddLabeledControl(layout, "Código",            _codeTextBox,       4);
            UiHelper.AddLabeledControl(layout, "Descripción",       _descriptionTextBox,5);
            UiHelper.AddLabeledControl(layout, "Notas",             _notesTextBox,      6);

            var saveButton   = UiHelper.CreatePrimaryButton("Guardar",   (_, _) => Save());
            var cancelButton = UiHelper.CreateSecondaryButton("Cancelar",(_, _) => Close());
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 56,
                Padding = new Padding(12)
            };
            buttons.Controls.Add(saveButton);
            buttons.Controls.Add(cancelButton);

            Controls.Add(layout);
            Controls.Add(buttons);

            LoadData();
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

            if (_diagnosis.Id > 0)
            {
                _patientCombo.SelectedValue = _diagnosis.PatientId;
                _professionalCombo.SelectedValue = _diagnosis.ProfessionalId;
            }

            LoadAttentions();

            if (_diagnosis.Id > 0)
            {
                _attentionCombo.SelectedValue = _diagnosis.AttentionId;
                _datePicker.Value = _diagnosis.DiagnosisDate;
                _codeTextBox.Text = _diagnosis.Code;
                _descriptionTextBox.Text = _diagnosis.Description;
                _notesTextBox.Text = _diagnosis.Notes;
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
            if (_patientCombo.SelectedValue is null || _professionalCombo.SelectedValue is null ||
                string.IsNullOrWhiteSpace(_descriptionTextBox.Text))
            {
                MessageBox.Show("Paciente, profesional y descripción son obligatorios.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var isNew = _diagnosis.Id == 0;
            _diagnosis.PatientId = Convert.ToInt32(_patientCombo.SelectedValue);
            _diagnosis.ProfessionalId = Convert.ToInt32(_professionalCombo.SelectedValue);
            _diagnosis.AttentionId = _attentionCombo.SelectedValue as int?;
            _diagnosis.DiagnosisDate = _datePicker.Value;
            _diagnosis.Code = _codeTextBox.Text.Trim();
            _diagnosis.Description = _descriptionTextBox.Text.Trim();
            _diagnosis.Notes = _notesTextBox.Text.Trim();

            _repository.Save(_diagnosis);
            _auditService.Log(_currentUser.Id, isNew ? "Crear" : "Actualizar", "Diagnóstico",
                _diagnosis.Id.ToString(), _diagnosis.Description);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

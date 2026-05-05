using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class ProfessionalsForm : Form
{
    private readonly User _currentUser;
    private readonly ProfessionalRepository _repository = new();
    private readonly AuditService _auditService = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly TextBox _searchTextBox = new() { PlaceholderText = "Buscar profesional", Width = 280 };
    private readonly CheckBox _showInactiveCheckBox = new() { AutoSize = true, Text = "Mostrar inactivos", Margin = new Padding(0, 8, 8, 0) };

    public ProfessionalsForm(User currentUser)
    {
        _currentUser = currentUser;
        Text = "Profesionales";
        BackColor = Color.White;

        BuildLayout();
        Load += (_, _) => ReloadData();
    }

    private void BuildLayout()
    {
        UiHelper.ConfigureGrid(_grid);
        _grid.DoubleClick += (_, _) => EditSelected();
        _searchTextBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) ReloadData(); };

        var editButton   = UiHelper.CreateSecondaryButton("Editar",             (_, _) => EditSelected());
        var toggleButton = UiHelper.CreateSecondaryButton("Activar / desactivar",(_, _) => ToggleActive());

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Profesionales");
        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8) };

        toolbar.Controls.Add(_searchTextBox);
        toolbar.Controls.Add(_showInactiveCheckBox);
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Buscar",            (_, _) => ReloadData()));
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Nuevo profesional", (_, _) => OpenEditor()));
        toolbar.Controls.Add(editButton);
        toolbar.Controls.Add(toggleButton);

        root.Controls.Add(_grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        _grid.DataSource = _repository.GetAll(_searchTextBox.Text, _showInactiveCheckBox.Checked);

        if (_grid.Columns.Count > 0)
        {
            _grid.Columns[nameof(Professional.Id)].HeaderText = "Id";
            _grid.Columns[nameof(Professional.FullName)].HeaderText = "Nombre";
            _grid.Columns[nameof(Professional.ProfessionalType)].HeaderText = "Tipo";
            _grid.Columns[nameof(Professional.Specialty)].HeaderText = "Especialidad";
            _grid.Columns[nameof(Professional.RegistrationNumber)].HeaderText = "Registro";
            _grid.Columns[nameof(Professional.Phone)].HeaderText = "Teléfono";
            _grid.Columns[nameof(Professional.Email)].HeaderText = "Correo";
            _grid.Columns[nameof(Professional.IsActive)].HeaderText = "Activo";
            _grid.Columns[nameof(Professional.CreatedAt)].Visible = false;
        }
    }

    private Professional? GetSelected() => _grid.CurrentRow?.DataBoundItem as Professional;

    private void EditSelected()
    {
        var professional = GetSelected();
        if (professional is null)
        {
            MessageBox.Show("Seleccione un profesional.", "Profesionales", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(professional.Id);
    }

    private void OpenEditor(int? professionalId = null)
    {
        var professional = professionalId.HasValue ? _repository.GetById(professionalId.Value) : null;
        using var dialog = new ProfessionalEditorDialog(professional);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
            return;

        _repository.Save(dialog.Result);
        _auditService.Log(_currentUser.Id, professionalId.HasValue ? "Actualizar" : "Crear", "Profesional",
            dialog.Result.Id.ToString(), dialog.Result.FullName);
        ReloadData();
    }

    private void ToggleActive()
    {
        var professional = GetSelected();
        if (professional is null)
        {
            MessageBox.Show("Seleccione un profesional.", "Profesionales", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _repository.SetActive(professional.Id, !professional.IsActive);
        _auditService.Log(_currentUser.Id, professional.IsActive ? "Desactivar" : "Activar", "Profesional",
            professional.Id.ToString(), professional.FullName);
        ReloadData();
    }

    private sealed class ProfessionalEditorDialog : Form
    {
        private readonly Professional _professional;
        private readonly TextBox _nameTextBox = new();
        private readonly TextBox _typeTextBox = new();
        private readonly TextBox _specialtyTextBox = new();
        private readonly TextBox _registrationTextBox = new();
        private readonly TextBox _phoneTextBox = new();
        private readonly TextBox _emailTextBox = new();
        private readonly CheckBox _activeCheckBox = new() { Text = "Profesional activo", Checked = true };

        public Professional? Result { get; private set; }

        public ProfessionalEditorDialog(Professional? professional)
        {
            _professional = professional ?? new Professional { CreatedAt = DateTime.Now, IsActive = true };

            Text = _professional.Id == 0 ? "Nuevo profesional" : "Editar profesional";
            Width = 620;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;

            var layout = UiHelper.CreateEditorLayout(7);
            UiHelper.AddLabeledControl(layout, "Nombre completo",    _nameTextBox,         0);
            UiHelper.AddLabeledControl(layout, "Tipo de profesional",_typeTextBox,         1);
            UiHelper.AddLabeledControl(layout, "Especialidad",       _specialtyTextBox,    2);
            UiHelper.AddLabeledControl(layout, "Registro profesional",_registrationTextBox,3);
            UiHelper.AddLabeledControl(layout, "Teléfono",           _phoneTextBox,        4);
            UiHelper.AddLabeledControl(layout, "Correo",             _emailTextBox,        5);
            UiHelper.AddLabeledControl(layout, "Estado",             _activeCheckBox,      6);

            var saveButton   = UiHelper.CreatePrimaryButton("Guardar", (_, _) => Save());
            var cancelButton = UiHelper.CreateSecondaryButton("Cancelar", (_, _) => Close());
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

            _nameTextBox.Text = _professional.FullName;
            _typeTextBox.Text = _professional.ProfessionalType;
            _specialtyTextBox.Text = _professional.Specialty;
            _registrationTextBox.Text = _professional.RegistrationNumber;
            _phoneTextBox.Text = _professional.Phone;
            _emailTextBox.Text = _professional.Email;
            _activeCheckBox.Checked = _professional.IsActive;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text) ||
                string.IsNullOrWhiteSpace(_typeTextBox.Text) ||
                string.IsNullOrWhiteSpace(_specialtyTextBox.Text))
            {
                MessageBox.Show("Nombre, tipo y especialidad son obligatorios.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!ValidationHelper.IsValidEmail(_emailTextBox.Text))
            {
                MessageBox.Show("El correo del profesional no es válido.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _professional.FullName = _nameTextBox.Text.Trim();
            _professional.ProfessionalType = _typeTextBox.Text.Trim();
            _professional.Specialty = _specialtyTextBox.Text.Trim();
            _professional.RegistrationNumber = _registrationTextBox.Text.Trim();
            _professional.Phone = _phoneTextBox.Text.Trim();
            _professional.Email = _emailTextBox.Text.Trim();
            _professional.IsActive = _activeCheckBox.Checked;

            Result = _professional;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class ServicesForm : Form
{
    private readonly User _currentUser;
    private readonly ServiceRepository _repository = new();
    private readonly AuditService _auditService = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly TextBox _searchTextBox = new() { PlaceholderText = "Buscar prestación", Width = 280 };
    private readonly CheckBox _showInactiveCheckBox = new() { AutoSize = true, Text = "Mostrar inactivas", Margin = new Padding(0, 8, 8, 0) };

    public ServicesForm(User currentUser)
    {
        _currentUser = currentUser;
        Text = "Prestaciones";
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
        var title = UiHelper.CreateSectionTitle("Prestaciones oftalmológicas");
        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8) };

        toolbar.Controls.Add(_searchTextBox);
        toolbar.Controls.Add(_showInactiveCheckBox);
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Buscar",          (_, _) => ReloadData()));
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Nueva prestación",(_, _) => OpenEditor()));
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
            _grid.Columns[nameof(OphthalmologyService.Id)].HeaderText = "Id";
            _grid.Columns[nameof(OphthalmologyService.Name)].HeaderText = "Nombre";
            _grid.Columns[nameof(OphthalmologyService.Description)].HeaderText = "Descripción";
            _grid.Columns[nameof(OphthalmologyService.Price)].HeaderText = "Precio";
            _grid.Columns[nameof(OphthalmologyService.DurationMinutes)].HeaderText = "Duración (min)";
            _grid.Columns[nameof(OphthalmologyService.IsActive)].HeaderText = "Activa";
        }
    }

    private OphthalmologyService? GetSelected() => _grid.CurrentRow?.DataBoundItem as OphthalmologyService;

    private void EditSelected()
    {
        var service = GetSelected();
        if (service is null)
        {
            MessageBox.Show("Seleccione una prestación.", "Prestaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(service.Id);
    }

    private void OpenEditor(int? serviceId = null)
    {
        var service = serviceId.HasValue ? _repository.GetById(serviceId.Value) : null;
        using var dialog = new ServiceEditorDialog(service);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
            return;

        _repository.Save(dialog.Result);
        _auditService.Log(_currentUser.Id, serviceId.HasValue ? "Actualizar" : "Crear", "Prestación",
            dialog.Result.Id.ToString(), dialog.Result.Name);
        ReloadData();
    }

    private void ToggleActive()
    {
        var service = GetSelected();
        if (service is null)
        {
            MessageBox.Show("Seleccione una prestación.", "Prestaciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _repository.SetActive(service.Id, !service.IsActive);
        _auditService.Log(_currentUser.Id, service.IsActive ? "Desactivar" : "Activar", "Prestación",
            service.Id.ToString(), service.Name);
        ReloadData();
    }

    private sealed class ServiceEditorDialog : Form
    {
        private readonly OphthalmologyService _service;
        private readonly TextBox _nameTextBox = new();
        private readonly TextBox _descriptionTextBox = new() { Multiline = true, Height = 80 };
        private readonly NumericUpDown _priceNumeric = new() { DecimalPlaces = 0, Maximum = 100000000 };
        private readonly NumericUpDown _durationNumeric = new() { Minimum = 5, Maximum = 240, Value = 30 };
        private readonly CheckBox _activeCheckBox = new() { Text = "Prestación activa", Checked = true };

        public OphthalmologyService? Result { get; private set; }

        public ServiceEditorDialog(OphthalmologyService? service)
        {
            _service = service ?? new OphthalmologyService { IsActive = true, DurationMinutes = 30 };

            Text = _service.Id == 0 ? "Nueva prestación" : "Editar prestación";
            Width = 620;
            Height = 360;
            StartPosition = FormStartPosition.CenterParent;

            var layout = UiHelper.CreateEditorLayout(5);
            UiHelper.AddLabeledControl(layout, "Nombre",           _nameTextBox,       0);
            UiHelper.AddLabeledControl(layout, "Descripción",      _descriptionTextBox,1);
            UiHelper.AddLabeledControl(layout, "Precio",           _priceNumeric,      2);
            UiHelper.AddLabeledControl(layout, "Duración (min)",   _durationNumeric,   3);
            UiHelper.AddLabeledControl(layout, "Estado",           _activeCheckBox,    4);

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

            _nameTextBox.Text = _service.Name;
            _descriptionTextBox.Text = _service.Description;
            _priceNumeric.Value = _service.Price;
            _durationNumeric.Value = _service.DurationMinutes;
            _activeCheckBox.Checked = _service.IsActive;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("El nombre de la prestación es obligatorio.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _service.Name = _nameTextBox.Text.Trim();
            _service.Description = _descriptionTextBox.Text.Trim();
            _service.Price = _priceNumeric.Value;
            _service.DurationMinutes = (int)_durationNumeric.Value;
            _service.IsActive = _activeCheckBox.Checked;

            Result = _service;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

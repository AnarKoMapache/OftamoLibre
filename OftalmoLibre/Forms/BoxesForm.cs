using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class BoxesForm : Form
{
    private readonly User _currentUser;
    private readonly BoxRepository _repository = new();
    private readonly AuditService _auditService = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly TextBox _searchTextBox = new() { PlaceholderText = "Buscar box", Width = 280 };
    private readonly CheckBox _showInactiveCheckBox = new() { AutoSize = true, Text = "Mostrar inactivos", Margin = new Padding(0, 8, 8, 0) };

    public BoxesForm(User currentUser)
    {
        _currentUser = currentUser;
        Text = "Boxes";
        BackColor = Color.White;

        BuildLayout();
        Load += (_, _) => ReloadData();
    }

    private void BuildLayout()
    {
        UiHelper.ConfigureGrid(_grid);
        _grid.DoubleClick += (_, _) => EditSelected();
        _searchTextBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) ReloadData(); };

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Boxes");
        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8) };

        toolbar.Controls.Add(_searchTextBox);
        toolbar.Controls.Add(_showInactiveCheckBox);
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Buscar", (_, _) => ReloadData()));
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Nuevo box", (_, _) => OpenEditor()));
        toolbar.Controls.Add(UiHelper.CreateSecondaryButton("Editar", (_, _) => EditSelected()));
        toolbar.Controls.Add(UiHelper.CreateSecondaryButton("Activar / desactivar", (_, _) => ToggleActive()));

        root.Controls.Add(_grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        _grid.DataSource = _repository.GetAll(_searchTextBox.Text, _showInactiveCheckBox.Checked);
        if (_grid.Columns.Count == 0)
        {
            return;
        }

        _grid.Columns[nameof(BoxLocation.Id)].HeaderText = "Id";
        _grid.Columns[nameof(BoxLocation.Name)].HeaderText = "Nombre";
        _grid.Columns[nameof(BoxLocation.IsActive)].HeaderText = "Activo";
    }

    private BoxLocation? GetSelected() => _grid.CurrentRow?.DataBoundItem as BoxLocation;

    private void EditSelected()
    {
        var box = GetSelected();
        if (box is null)
        {
            MessageBox.Show("Seleccione un box.", "Boxes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        OpenEditor(box.Id);
    }

    private void OpenEditor(int? boxId = null)
    {
        var box = boxId.HasValue ? _repository.GetById(boxId.Value) : null;
        using var dialog = new BoxEditorDialog(box);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
        {
            return;
        }

        _repository.Save(dialog.Result, dialog.OriginalName);
        _auditService.Log(_currentUser.Id, boxId.HasValue ? "Actualizar" : "Crear", "Box", dialog.Result.Id.ToString(), dialog.Result.Name);
        ReloadData();
    }

    private void ToggleActive()
    {
        var box = GetSelected();
        if (box is null)
        {
            MessageBox.Show("Seleccione un box.", "Boxes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _repository.SetActive(box.Id, !box.IsActive);
        _auditService.Log(_currentUser.Id, box.IsActive ? "Desactivar" : "Activar", "Box", box.Id.ToString(), box.Name);
        ReloadData();
    }

    private sealed class BoxEditorDialog : Form
    {
        private readonly BoxLocation _box;
        private readonly TextBox _nameTextBox = new();
        private readonly CheckBox _activeCheckBox = new() { Text = "Box activo", Checked = true };

        public BoxLocation? Result { get; private set; }
        public string OriginalName { get; }

        public BoxEditorDialog(BoxLocation? box)
        {
            _box = box ?? new BoxLocation { IsActive = true };
            OriginalName = _box.Name;

            Text = _box.Id == 0 ? "Nuevo box" : "Editar box";
            Width = 480;
            Height = 220;
            StartPosition = FormStartPosition.CenterParent;

            var layout = UiHelper.CreateEditorLayout(2);
            UiHelper.AddLabeledControl(layout, "Nombre", _nameTextBox, 0);
            UiHelper.AddLabeledControl(layout, "Estado", _activeCheckBox, 1);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Height = 56,
                Padding = new Padding(12)
            };
            buttons.Controls.Add(UiHelper.CreatePrimaryButton("Guardar", (_, _) => Save()));
            buttons.Controls.Add(UiHelper.CreateSecondaryButton("Cancelar", (_, _) => Close()));

            Controls.Add(layout);
            Controls.Add(buttons);

            _nameTextBox.Text = _box.Name;
            _activeCheckBox.Checked = _box.IsActive;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
            {
                MessageBox.Show("El nombre del box es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _box.Name = _nameTextBox.Text.Trim();
            _box.IsActive = _activeCheckBox.Checked;
            Result = _box;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

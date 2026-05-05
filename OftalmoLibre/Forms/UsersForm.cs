using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class UsersForm : Form
{
    private readonly User _currentUser;
    private readonly UserRepository _repository = new();
    private readonly AuthService _authService = new();
    private readonly AuditService _auditService = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly TextBox _searchTextBox = new() { PlaceholderText = "Buscar usuario", Width = 250 };

    public UsersForm(User currentUser)
    {
        _currentUser = currentUser;
        Text = "Usuarios";
        BackColor = Color.White;

        BuildLayout();
        Load += (_, _) => ReloadData();
    }

    private void BuildLayout()
    {
        UiHelper.ConfigureGrid(_grid);
        _grid.DoubleClick += (_, _) => EditSelected();
        _searchTextBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) ReloadData(); };

        var editButton = UiHelper.CreateSecondaryButton("Editar", (_, _) => EditSelected());

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Usuarios del sistema");
        var toolbar = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Top, Padding = new Padding(0, 0, 0, 8) };

        toolbar.Controls.Add(_searchTextBox);
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Buscar",       (_, _) => ReloadData()));
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Nuevo usuario",(_, _) => OpenEditor()));
        toolbar.Controls.Add(editButton);

        root.Controls.Add(_grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        _grid.DataSource = _repository.GetAll(_searchTextBox.Text);
        if (_grid.Columns.Count > 0)
        {
            _grid.Columns[nameof(User.Id)].HeaderText = "Id";
            _grid.Columns[nameof(User.Username)].HeaderText = "Usuario";
            _grid.Columns[nameof(User.FullName)].HeaderText = "Nombre";
            _grid.Columns[nameof(User.Role)].HeaderText = "Rol";
            _grid.Columns[nameof(User.IsActive)].HeaderText = "Activo";
            _grid.Columns[nameof(User.MustChangePassword)].HeaderText = "Debe cambiar clave";
            _grid.Columns[nameof(User.PasswordHash)].Visible = false;
            _grid.Columns[nameof(User.CreatedAt)].HeaderText = "Creado";
            _grid.Columns[nameof(User.UpdatedAt)].HeaderText = "Actualizado";
        }
    }

    private User? GetSelected() => _grid.CurrentRow?.DataBoundItem as User;

    private void EditSelected()
    {
        var user = GetSelected();
        if (user is null)
        {
            MessageBox.Show("Seleccione un usuario.", "Usuarios", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(user.Id);
    }

    private void OpenEditor(int? userId = null)
    {
        var user = userId.HasValue ? _repository.GetById(userId.Value) : null;
        using var dialog = new UserEditorDialog(_repository, _authService, user);
        if (dialog.ShowDialog(this) != DialogResult.OK || dialog.Result is null)
            return;

        _repository.Save(dialog.Result);
        _auditService.Log(_currentUser.Id, userId.HasValue ? "Actualizar" : "Crear", "Usuario",
            dialog.Result.Id.ToString(), dialog.Result.Username);
        ReloadData();
    }

    private sealed class UserEditorDialog : Form
    {
        private readonly UserRepository _repository;
        private readonly User _user;
        private readonly TextBox _usernameTextBox = new();
        private readonly TextBox _fullNameTextBox = new();
        private readonly ComboBox _roleCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        private readonly TextBox _passwordTextBox = new() { UseSystemPasswordChar = true };
        private readonly CheckBox _activeCheckBox = new() { Text = "Usuario activo", Checked = true };
        private readonly CheckBox _mustChangePasswordCheckBox = new() { Text = "Debe cambiar contraseña al ingresar" };

        public User? Result { get; private set; }

        public UserEditorDialog(UserRepository repository, AuthService authService, User? user)
        {
            _repository = repository;
            _user = user ?? new User { CreatedAt = DateTime.Now, IsActive = true, MustChangePassword = false };

            Text = _user.Id == 0 ? "Nuevo usuario" : "Editar usuario";
            Width = 620;
            Height = 360;
            StartPosition = FormStartPosition.CenterParent;

            _roleCombo.Items.AddRange(authService.Roles.Cast<object>().ToArray());

            var layout = UiHelper.CreateEditorLayout(6);
            UiHelper.AddLabeledControl(layout, "Usuario",     _usernameTextBox, 0);
            UiHelper.AddLabeledControl(layout, "Nombre completo", _fullNameTextBox, 1);
            UiHelper.AddLabeledControl(layout, "Rol",         _roleCombo,       2);
            UiHelper.AddLabeledControl(layout, _user.Id == 0 ? "Contraseña" : "Nueva contraseña (opcional)", _passwordTextBox, 3);
            UiHelper.AddLabeledControl(layout, "Estado",      _activeCheckBox,  4);
            UiHelper.AddLabeledControl(layout, "Seguridad",   _mustChangePasswordCheckBox, 5);

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

            _usernameTextBox.Text = _user.Username;
            _fullNameTextBox.Text = _user.FullName;
            _roleCombo.SelectedItem = _user.Role;
            if (_roleCombo.SelectedIndex < 0 && _roleCombo.Items.Count > 0)
                _roleCombo.SelectedIndex = 0;
            _activeCheckBox.Checked = _user.IsActive;
            _mustChangePasswordCheckBox.Checked = _user.MustChangePassword;
        }

        private void Save()
        {
            if (string.IsNullOrWhiteSpace(_usernameTextBox.Text) ||
                string.IsNullOrWhiteSpace(_fullNameTextBox.Text) ||
                _roleCombo.SelectedItem is null)
            {
                MessageBox.Show("Usuario, nombre y rol son obligatorios.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existing = _repository.GetByUsername(_usernameTextBox.Text.Trim());
            if (existing is not null && existing.Id != _user.Id)
            {
                MessageBox.Show("Ya existe otro usuario con ese nombre.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_user.Id == 0 && string.IsNullOrWhiteSpace(_passwordTextBox.Text))
            {
                MessageBox.Show("Debe ingresar una contraseña para el nuevo usuario.", "Validación",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _user.Username = _usernameTextBox.Text.Trim();
            _user.FullName = _fullNameTextBox.Text.Trim();
            _user.Role = _roleCombo.SelectedItem.ToString() ?? "Solo lectura";
            _user.IsActive = _activeCheckBox.Checked;
            _user.MustChangePassword = _mustChangePasswordCheckBox.Checked;

            if (!string.IsNullOrWhiteSpace(_passwordTextBox.Text))
                _user.PasswordHash = PasswordHelper.HashPassword(_passwordTextBox.Text);

            Result = _user;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class LoginForm : Form
{
    private readonly AuthService _authService = new();
    private readonly CenterConfigRepository _centerConfigRepository = new();
    private readonly TextBox _usernameTextBox = new() { PlaceholderText = "Usuario" };
    private readonly TextBox _passwordTextBox = new() { PlaceholderText = "Contraseña", UseSystemPasswordChar = true };

    public User? AuthenticatedUser { get; private set; }

    public LoginForm()
    {
        var centerConfig = _centerConfigRepository.Get();

        Text = $"{centerConfig.CenterName} — Iniciar sesión";
        Width = 380;
        Height = 340;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        var header = new Panel
        {
            BackColor = Color.FromArgb(34, 40, 49),
            Dock = DockStyle.Top,
            Height = 90
        };

        var subtitleLbl = UiHelper.CreateDarkLabel(AppIdentity.AppSubtitle, 8.5F,
            foreColor: Color.FromArgb(160, 174, 192));
        subtitleLbl.Dock = DockStyle.Bottom;
        subtitleLbl.Height = 22;
        subtitleLbl.Padding = new Padding(22, 0, 0, 6);

        var centerLbl = UiHelper.CreateDarkLabel(centerConfig.CenterName, 13F, FontStyle.Bold);
        centerLbl.Dock = DockStyle.Top;
        centerLbl.Height = 48;
        centerLbl.Padding = new Padding(22, 16, 0, 0);

        header.Controls.Add(subtitleLbl);
        header.Controls.Add(centerLbl);
        UiHelper.ForcePanelBackground(header);

        var body = new TableLayoutPanel
        {
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 14, 22, 16),
            RowCount = 5
        };

        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        body.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var usernameLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = UiHelper.TextSecondary,
            Margin = new Padding(0, 0, 0, 2),
            Text = "Usuario"
        };
        var passwordLabel = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
            ForeColor = UiHelper.TextSecondary,
            Margin = new Padding(0, 8, 0, 2),
            Text = "Contraseña"
        };

        _usernameTextBox.Dock = DockStyle.Top;
        _usernameTextBox.Margin = new Padding(0);

        _passwordTextBox.Dock = DockStyle.Top;
        _passwordTextBox.Margin = new Padding(0);

        var loginButton = UiHelper.CreatePrimaryButton("Ingresar", (_, _) => TryLogin());
        loginButton.Dock = DockStyle.Top;
        loginButton.Height = 38;
        loginButton.Margin = new Padding(0, 14, 0, 0);

        body.Controls.Add(usernameLabel,    0, 0);
        body.Controls.Add(_usernameTextBox, 0, 1);
        body.Controls.Add(passwordLabel,    0, 2);
        body.Controls.Add(_passwordTextBox, 0, 3);
        body.Controls.Add(loginButton,      0, 4);

        Controls.Add(body);
        Controls.Add(header);

        AcceptButton = loginButton;
    }

    private void TryLogin()
    {
        var username = _usernameTextBox.Text.Trim();
        var password = _passwordTextBox.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show("Debe ingresar usuario y contraseña.", "Validación",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var user = _authService.Login(username, password);
        if (user is null)
        {
            MessageBox.Show("Usuario o contraseña incorrectos.", "Acceso denegado",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            _passwordTextBox.Clear();
            _passwordTextBox.Focus();
            return;
        }

        if (user.MustChangePassword)
        {
            MessageBox.Show(
                "Está ingresando con el usuario administrador inicial.\nSe recomienda cambiar la contraseña lo antes posible.",
                "Advertencia de seguridad",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        AuthenticatedUser = user;
        DialogResult = DialogResult.OK;
        Close();
    }
}

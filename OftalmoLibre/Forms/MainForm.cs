using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Forms;

public sealed class MainForm : Form
{
    private readonly User _currentUser;
    private readonly CenterConfigRepository _centerConfigRepository = new();
    private readonly Panel _contentPanel = new() { Dock = DockStyle.Fill, BackColor = UiHelper.ContentBg };
    private readonly Button _appointmentsButton = new();
    private readonly Button _prescriptionsButton = new();
    private Form? _currentView;

    public bool LogoutRequested { get; private set; }

    public MainForm(User currentUser)
    {
        _currentUser = currentUser;
        var centerName = _centerConfigRepository.Get().CenterName;

        Text = centerName;
        WindowState = FormWindowState.Maximized;
        StartPosition = FormStartPosition.CenterScreen;

        BuildLayout(centerName);
        Load += (_, _) => OpenAppointments();
    }

    private void BuildLayout(string centerName)
    {
        var header = new Panel
        {
            BackColor = UiHelper.SidebarBg,
            Dock = DockStyle.Top,
            Height = 76
        };
        UiHelper.ForcePanelBackground(header);

        var titleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Left,
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.White,
            Width = 360,
            Padding = new Padding(22, 14, 0, 0),
            Text = centerName
        };

        var subtitleLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Left,
            Font = new Font("Segoe UI", 9F),
            ForeColor = UiHelper.SidebarMuted,
            Width = 270,
            Padding = new Padding(0, 20, 0, 0),
            Text = AppIdentity.AppSubtitle
        };

        var navPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Left,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(10, 18, 0, 0),
            WrapContents = false
        };
        navPanel.Controls.AddRange(
        [
            ConfigureNavButton(_appointmentsButton, "Citas", (_, _) => OpenAppointments()),
            ConfigureNavButton(_prescriptionsButton, "Recetas", (_, _) => OpenPrescriptions())
        ]);

        var logoutButton = new Button
        {
            BackColor = Color.FromArgb(44, 51, 61),
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Width = 130,
            Text = "Cerrar sesión",
            UseVisualStyleBackColor = false
        };
        logoutButton.FlatAppearance.BorderSize = 0;
        logoutButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 40, 40);
        logoutButton.Click += (_, _) =>
        {
            if (MessageBox.Show("¿Desea cerrar la sesión actual?", "Cerrar sesión",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            LogoutRequested = true;
            Close();
        };

        var userInfoLabel = new Label
        {
            AutoSize = false,
            Dock = DockStyle.Right,
            Font = new Font("Segoe UI", 9F),
            ForeColor = UiHelper.SidebarMuted,
            TextAlign = ContentAlignment.MiddleRight,
            Width = 220,
            Padding = new Padding(0, 0, 18, 0),
            Text = $"{_currentUser.FullName} · {_currentUser.Role}"
        };

        header.Controls.Add(logoutButton);
        header.Controls.Add(userInfoLabel);
        header.Controls.Add(navPanel);
        header.Controls.Add(subtitleLabel);
        header.Controls.Add(titleLabel);

        Controls.Add(_contentPanel);
        Controls.Add(header);
    }

    private static Button ConfigureNavButton(Button button, string text, EventHandler onClick)
    {
        button.AutoSize = true;
        button.BackColor = Color.FromArgb(44, 51, 61);
        button.FlatStyle = FlatStyle.Flat;
        button.ForeColor = Color.White;
        button.Height = 34;
        button.Margin = new Padding(0, 0, 8, 0);
        button.MinimumSize = new Size(90, 34);
        button.Padding = new Padding(10, 0, 10, 0);
        button.Text = text;
        button.UseVisualStyleBackColor = false;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(58, 68, 82);
        button.Click += onClick;
        return button;
    }

    private void OpenAppointments()
    {
        SetActiveNavButton(_appointmentsButton);
        ShowView(new AgendaForm(_currentUser));
    }

    private void OpenPrescriptions()
    {
        SetActiveNavButton(_prescriptionsButton);
        ShowView(new PrescriptionsForm(_currentUser));
    }

    private void ShowView(Form view)
    {
        _currentView?.Close();
        _currentView?.Dispose();

        _currentView = view;
        _currentView.TopLevel = false;
        _currentView.FormBorderStyle = FormBorderStyle.None;
        _currentView.Dock = DockStyle.Fill;

        _contentPanel.Controls.Clear();
        _contentPanel.Controls.Add(_currentView);
        _currentView.Show();
    }

    private void SetActiveNavButton(Button activeButton)
    {
        foreach (var button in new[] { _appointmentsButton, _prescriptionsButton })
        {
            button.BackColor = ReferenceEquals(button, activeButton)
                ? UiHelper.SidebarBtnActive
                : Color.FromArgb(44, 51, 61);
        }
    }
}

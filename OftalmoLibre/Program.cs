using OftalmoLibre.Data;
using OftalmoLibre.Forms;

namespace OftalmoLibre;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        DatabaseInitializer.Initialize();

        var keepRunning = true;

        while (keepRunning)
        {
            using var loginForm = new LoginForm();
            if (loginForm.ShowDialog() != DialogResult.OK || loginForm.AuthenticatedUser is null)
            {
                break;
            }

            using var mainForm = new MainForm(loginForm.AuthenticatedUser);
            Application.Run(mainForm);
            keepRunning = mainForm.LogoutRequested;
        }
    }
}

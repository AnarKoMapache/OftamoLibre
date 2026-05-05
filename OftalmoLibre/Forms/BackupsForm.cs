using System.Diagnostics;
using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class BackupsForm : Form
{
    private readonly User _currentUser;
    private readonly BackupService _backupService = new();
    private readonly AuditService _auditService = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };

    public BackupsForm(User currentUser)
    {
        _currentUser = currentUser;
        Text = "Backups";
        BackColor = Color.White;

        BuildLayout();
        Load += (_, _) => ReloadData();
    }

    private void BuildLayout()
    {
        UiHelper.ConfigureGrid(_grid);
        _grid.DoubleClick += (_, _) => OpenSelected();

        var openButton    = UiHelper.CreateSecondaryButton("Abrir archivo", (_, _) => OpenSelected());
        var refreshButton = UiHelper.CreateSecondaryButton("Actualizar",    (_, _) => ReloadData());

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Respaldos de base de datos");
        var toolbar = UiHelper.CreateToolbar(
            UiHelper.CreatePrimaryButton("Crear backup", (_, _) => CreateBackup()),
            openButton,
            refreshButton);

        root.Controls.Add(_grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        _grid.DataSource = _backupService.GetBackups();
        if (_grid.Columns.Count > 0)
        {
            _grid.Columns[nameof(BackupRecord.Id)].HeaderText = "Id";
            _grid.Columns[nameof(BackupRecord.FileName)].HeaderText = "Archivo";
            _grid.Columns[nameof(BackupRecord.FullPath)].HeaderText = "Ruta";
            _grid.Columns[nameof(BackupRecord.CreatedAt)].HeaderText = "Fecha";
            _grid.Columns[nameof(BackupRecord.Notes)].HeaderText = "Notas";
        }
    }

    private BackupRecord? GetSelected() => _grid.CurrentRow?.DataBoundItem as BackupRecord;

    private void CreateBackup()
    {
        var backup = _backupService.CreateBackup("Backup manual");
        _auditService.Log(_currentUser.Id, "Crear", "Backup", backup.Id.ToString(), backup.FileName);
        ReloadData();
        MessageBox.Show("Backup creado correctamente.", "Backups", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenSelected()
    {
        var backup = GetSelected();
        if (backup is null)
        {
            MessageBox.Show("Seleccione un backup.", "Backups", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            if (OperatingSystem.IsWindows())
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{backup.FullPath}\"") { UseShellExecute = true });
            else
                Process.Start(new ProcessStartInfo("xdg-open", Path.GetDirectoryName(backup.FullPath) ?? ".") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"No fue posible abrir la ubicación del backup.\n{ex.Message}", "Backups",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}

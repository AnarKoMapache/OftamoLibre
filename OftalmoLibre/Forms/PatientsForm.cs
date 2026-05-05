using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class PatientsForm : Form
{
    private readonly User _currentUser;
    private readonly PatientRepository _repository = new();
    private readonly AuditService _auditService = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly TextBox _searchTextBox = new() { PlaceholderText = "Buscar por nombre, ficha o RUT/DNI", Width = 280 };
    private readonly CheckBox _showInactiveCheckBox = new() { AutoSize = true, Text = "Mostrar inactivos", Margin = new Padding(0, 8, 8, 0) };
    private readonly bool _openCreateOnShown;
    private bool _createOpened;

    public PatientsForm(User currentUser, bool openCreateOnShown = false)
    {
        _currentUser = currentUser;
        _openCreateOnShown = openCreateOnShown;

        Text = "Clientes";
        BackColor = Color.White;

        BuildLayout();

        Load += (_, _) => ReloadData();
        Shown += (_, _) =>
        {
            if (_openCreateOnShown && !_createOpened)
            {
                _createOpened = true;
                OpenEditor();
            }
        };
    }

    private void BuildLayout()
    {
        UiHelper.ConfigureGrid(_grid);
        _grid.DoubleClick += (_, _) => OpenProfile();
        _searchTextBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) ReloadData(); };

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Clientes");

        var editButton    = UiHelper.CreateSecondaryButton("Editar",             (_, _) => EditSelected());
        var profileButton = UiHelper.CreateSecondaryButton("Ver ficha",          (_, _) => OpenProfile());
        var toggleButton  = UiHelper.CreateSecondaryButton("Activar / desactivar",(_, _) => ToggleActive());

        var toolbar = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(0, 0, 0, 8)
        };

        toolbar.Controls.Add(_searchTextBox);
        toolbar.Controls.Add(_showInactiveCheckBox);
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Buscar",         (_, _) => ReloadData()));
        toolbar.Controls.Add(UiHelper.CreatePrimaryButton("Nuevo cliente",  (_, _) => OpenEditor()));
        toolbar.Controls.Add(editButton);
        toolbar.Controls.Add(profileButton);
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
            _grid.Columns[nameof(Patient.Id)].HeaderText = "Id";
            _grid.Columns[nameof(Patient.RecordNumber)].HeaderText = "Ficha";
            _grid.Columns[nameof(Patient.FullName)].HeaderText = "Cliente";
            _grid.Columns[nameof(Patient.DocumentNumber)].HeaderText = "RUT/DNI";
            _grid.Columns[nameof(Patient.BirthDate)].HeaderText = "Nacimiento";
            _grid.Columns[nameof(Patient.Age)].HeaderText = "Edad";
            _grid.Columns[nameof(Patient.Phone1)].HeaderText = "Teléfono";
            _grid.Columns[nameof(Patient.Email)].HeaderText = "Correo";
            _grid.Columns[nameof(Patient.Insurance)].HeaderText = "Previsión";
            _grid.Columns[nameof(Patient.IsActive)].HeaderText = "Activo";

            foreach (var col in new[]
                     {
                         nameof(Patient.Phone2), nameof(Patient.Address), nameof(Patient.Occupation),
                         nameof(Patient.MedicalHistory), nameof(Patient.OphthalmicHistory),
                         nameof(Patient.UsesGlasses), nameof(Patient.ContactLenses),
                         nameof(Patient.Allergies), nameof(Patient.CurrentMedications),
                         nameof(Patient.GeneralNotes), nameof(Patient.CreatedAt)
                     })
            {
                if (_grid.Columns.Contains(col))
                    _grid.Columns[col].Visible = false;
            }
        }
    }

    private Patient? GetSelected() => _grid.CurrentRow?.DataBoundItem as Patient;

    private void EditSelected()
    {
        var patient = GetSelected();
        if (patient is null)
        {
            MessageBox.Show("Seleccione un cliente.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(patient.Id);
    }

    private void OpenEditor(int? patientId = null)
    {
        Patient? patient = patientId.HasValue ? _repository.GetById(patientId.Value) : null;
        using var form = new PatientEditorForm(_currentUser, patient);
        if (form.ShowDialog(this) == DialogResult.OK)
            ReloadData();
    }

    private void OpenProfile()
    {
        var patient = GetSelected();
        if (patient is null)
        {
            MessageBox.Show("Seleccione un cliente.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        using var form = new PatientProfileForm(patient.Id);
        form.ShowDialog(this);
    }

    private void ToggleActive()
    {
        var patient = GetSelected();
        if (patient is null)
        {
            MessageBox.Show("Seleccione un cliente.", "Clientes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        _repository.SetActive(patient.Id, !patient.IsActive);
        _auditService.Log(_currentUser.Id, patient.IsActive ? "Desactivar" : "Activar", "Cliente", patient.Id.ToString(), patient.FullName);
        ReloadData();
    }
}

using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Forms;

public sealed class PrescriptionsForm : Form
{
    private readonly User _currentUser;
    private readonly PrescriptionRepository _repository = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly TextBox _searchTextBox = new() { PlaceholderText = "Buscar por RUT, cliente u observaciones", Width = 280 };
    private readonly bool _openCreateOnShown;
    private bool _createOpened;

    public PrescriptionsForm(User currentUser, bool openCreateOnShown = false)
    {
        _currentUser = currentUser;
        _openCreateOnShown = openCreateOnShown;

        Text = "Recetas oftalmológicas";
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
        _grid.DoubleClick += (_, _) => EditSelected();
        _searchTextBox.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) ReloadData(); };

        var editButton    = UiHelper.CreateSecondaryButton("Editar",    (_, _) => EditSelected());
        var refreshButton = UiHelper.CreateSecondaryButton("Actualizar",(_, _) => ReloadData());

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Recetas oftalmológicas");
        var toolbar = UiHelper.CreateToolbar(
            _searchTextBox,
            UiHelper.CreatePrimaryButton("Buscar",       (_, _) => ReloadData()),
            UiHelper.CreatePrimaryButton("Nueva receta", (_, _) => OpenEditor()),
            editButton,
            refreshButton);

        root.Controls.Add(_grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        var search = string.IsNullOrWhiteSpace(_searchTextBox.Text) ? null : _searchTextBox.Text;
        _grid.DataSource = _repository.GetAll(search);
        if (_grid.Columns.Count > 0)
        {
            _grid.Columns[nameof(PrescriptionListItem.Id)].HeaderText = "Id";
            _grid.Columns[nameof(PrescriptionListItem.PrescriptionDate)].HeaderText = "Fecha";
            _grid.Columns[nameof(PrescriptionListItem.DocumentNumber)].HeaderText = "RUT";
            _grid.Columns[nameof(PrescriptionListItem.PatientName)].HeaderText = "Cliente";
            _grid.Columns[nameof(PrescriptionListItem.ProfessionalName)].HeaderText = "Profesional";
            _grid.Columns[nameof(PrescriptionListItem.Notes)].HeaderText = "Observaciones";
            _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
    }

    private PrescriptionListItem? GetSelected() => _grid.CurrentRow?.DataBoundItem as PrescriptionListItem;

    private void EditSelected()
    {
        var item = GetSelected();
        if (item is null)
        {
            MessageBox.Show("Seleccione una receta.", "Recetas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(item.Id);
    }

    private void OpenEditor(int? prescriptionId = null)
    {
        var prescription = prescriptionId.HasValue ? _repository.GetById(prescriptionId.Value) : null;
        using var form = new PrescriptionEditorForm(_currentUser, prescription);
        if (form.ShowDialog(this) == DialogResult.OK)
            ReloadData();
    }
}

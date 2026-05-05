using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Forms;

public sealed class AttentionsForm : Form
{
    private readonly User _currentUser;
    private readonly AttentionRepository _repository = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly TextBox _searchTextBox = new() { PlaceholderText = "Buscar por cliente o motivo", Width = 280 };
    private readonly bool _openCreateOnShown;
    private bool _createOpened;

    public AttentionsForm(User currentUser, bool openCreateOnShown = false)
    {
        _currentUser = currentUser;
        _openCreateOnShown = openCreateOnShown;

        Text = "Atenciones";
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

        var editButton     = UiHelper.CreateSecondaryButton("Editar",    (_, _) => EditSelected());
        var refreshButton  = UiHelper.CreateSecondaryButton("Actualizar",(_, _) => ReloadData());

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Atenciones");
        var toolbar = UiHelper.CreateToolbar(
            _searchTextBox,
            UiHelper.CreatePrimaryButton("Buscar",          (_, _) => ReloadData()),
            UiHelper.CreatePrimaryButton("Nueva atención",  (_, _) => OpenEditor()),
            editButton,
            refreshButton);

        root.Controls.Add(_grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        _grid.DataSource = _repository.GetAll(string.IsNullOrWhiteSpace(_searchTextBox.Text) ? null : _searchTextBox.Text);
        if (_grid.Columns.Count > 0)
        {
            _grid.Columns[nameof(AttentionListItem.Id)].HeaderText = "Id";
            _grid.Columns[nameof(AttentionListItem.VisitDate)].HeaderText = "Fecha";
            _grid.Columns[nameof(AttentionListItem.PatientName)].HeaderText = "Cliente";
            _grid.Columns[nameof(AttentionListItem.ProfessionalName)].HeaderText = "Profesional";
            _grid.Columns[nameof(AttentionListItem.ChiefComplaint)].HeaderText = "Motivo";
        }
    }

    private AttentionListItem? GetSelected() => _grid.CurrentRow?.DataBoundItem as AttentionListItem;

    private void EditSelected()
    {
        var item = GetSelected();
        if (item is null)
        {
            MessageBox.Show("Seleccione una atención.", "Atenciones", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(item.Id);
    }

    private void OpenEditor(int? attentionId = null)
    {
        var attention = attentionId.HasValue ? _repository.GetById(attentionId.Value) : null;
        using var form = new AttentionEditorForm(_currentUser, attention);
        if (form.ShowDialog(this) == DialogResult.OK)
            ReloadData();
    }
}

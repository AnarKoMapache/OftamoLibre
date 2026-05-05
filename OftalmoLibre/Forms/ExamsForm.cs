using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Forms;

public sealed class ExamsForm : Form
{
    private readonly User _currentUser;
    private readonly ExamRepository _repository = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly bool _openCreateOnShown;
    private bool _createOpened;

    public ExamsForm(User currentUser, bool openCreateOnShown = false)
    {
        _currentUser = currentUser;
        _openCreateOnShown = openCreateOnShown;

        Text = "Exámenes";
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

        var editButton    = UiHelper.CreateSecondaryButton("Editar",    (_, _) => EditSelected());
        var refreshButton = UiHelper.CreateSecondaryButton("Actualizar",(_, _) => ReloadData());

        var root = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        var title = UiHelper.CreateSectionTitle("Exámenes oftalmológicos");
        var toolbar = UiHelper.CreateToolbar(
            UiHelper.CreatePrimaryButton("Nuevo examen", (_, _) => OpenEditor()),
            editButton,
            refreshButton);

        root.Controls.Add(_grid);
        root.Controls.Add(toolbar);
        root.Controls.Add(title);
        Controls.Add(root);
    }

    private void ReloadData()
    {
        _grid.DataSource = _repository.GetAll();
        if (_grid.Columns.Count > 0)
        {
            _grid.Columns[nameof(ExamListItem.Id)].HeaderText = "Id";
            _grid.Columns[nameof(ExamListItem.ExamDate)].HeaderText = "Fecha";
            _grid.Columns[nameof(ExamListItem.PatientName)].HeaderText = "Paciente";
            _grid.Columns[nameof(ExamListItem.ProfessionalName)].HeaderText = "Profesional";
            _grid.Columns[nameof(ExamListItem.ExamType)].HeaderText = "Tipo de examen";
        }
    }

    private ExamListItem? GetSelected() => _grid.CurrentRow?.DataBoundItem as ExamListItem;

    private void EditSelected()
    {
        var item = GetSelected();
        if (item is null)
        {
            MessageBox.Show("Seleccione un examen.", "Exámenes", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(item.Id);
    }

    private void OpenEditor(int? examId = null)
    {
        var exam = examId.HasValue ? _repository.GetById(examId.Value) : null;
        using var form = new ExamEditorForm(_currentUser, exam);
        if (form.ShowDialog(this) == DialogResult.OK)
            ReloadData();
    }
}

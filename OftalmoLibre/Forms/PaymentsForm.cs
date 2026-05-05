using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Forms;

public sealed class PaymentsForm : Form
{
    private readonly User _currentUser;
    private readonly PaymentRepository _repository = new();
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly bool _openCreateOnShown;
    private bool _createOpened;

    public PaymentsForm(User currentUser, bool openCreateOnShown = false)
    {
        _currentUser = currentUser;
        _openCreateOnShown = openCreateOnShown;

        Text = "Pagos";
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
        var title = UiHelper.CreateSectionTitle("Pagos");
        var toolbar = UiHelper.CreateToolbar(
            UiHelper.CreatePrimaryButton("Registrar pago", (_, _) => OpenEditor()),
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
            _grid.Columns[nameof(PaymentListItem.Id)].HeaderText = "Id";
            _grid.Columns[nameof(PaymentListItem.PaymentDate)].HeaderText = "Fecha";
            _grid.Columns[nameof(PaymentListItem.PatientName)].HeaderText = "Paciente";
            _grid.Columns[nameof(PaymentListItem.Amount)].HeaderText = "Monto";
            _grid.Columns[nameof(PaymentListItem.Method)].HeaderText = "Método";
            _grid.Columns[nameof(PaymentListItem.Reference)].HeaderText = "Referencia";
        }
    }

    private PaymentListItem? GetSelected() => _grid.CurrentRow?.DataBoundItem as PaymentListItem;

    private void EditSelected()
    {
        var item = GetSelected();
        if (item is null)
        {
            MessageBox.Show("Seleccione un pago.", "Pagos", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        OpenEditor(item.Id);
    }

    private void OpenEditor(int? paymentId = null)
    {
        var payment = paymentId.HasValue ? _repository.GetById(paymentId.Value) : null;
        using var form = new PaymentForm(_currentUser, payment);
        if (form.ShowDialog(this) == DialogResult.OK)
            ReloadData();
    }
}

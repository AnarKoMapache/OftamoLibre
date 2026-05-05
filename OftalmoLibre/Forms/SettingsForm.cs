using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class SettingsForm : Form
{
    private readonly User _currentUser;
    private readonly CenterConfigRepository _repository = new();
    private readonly AuditService _auditService = new();
    private readonly TextBox _centerNameTextBox = new();
    private readonly TextBox _addressTextBox = new() { Multiline = true, Height = 70 };
    private readonly TextBox _phoneTextBox = new();
    private readonly TextBox _emailTextBox = new();
    private readonly TextBox _currencyTextBox = new();

    public SettingsForm(User currentUser)
    {
        _currentUser = currentUser;
        Text = "Configuración de la óptica";
        BackColor = Color.White;

        BuildLayout();
        Load += (_, _) => LoadData();
    }

    private void BuildLayout()
    {
        var layout = UiHelper.CreateEditorLayout(5);
        UiHelper.AddLabeledControl(layout, "Nombre de la óptica", _centerNameTextBox, 0);
        UiHelper.AddLabeledControl(layout, "Dirección", _addressTextBox, 1);
        UiHelper.AddLabeledControl(layout, "Teléfono", _phoneTextBox, 2);
        UiHelper.AddLabeledControl(layout, "Correo", _emailTextBox, 3);
        UiHelper.AddLabeledControl(layout, "Moneda por defecto", _currencyTextBox, 4);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 56, Padding = new Padding(12) };
        var saveButton = UiHelper.CreatePrimaryButton("Guardar datos de la óptica", (_, _) => Save());
        buttons.Controls.Add(saveButton);

        Controls.Add(layout);
        Controls.Add(buttons);
    }

    private void LoadData()
    {
        var config = _repository.Get();
        _centerNameTextBox.Text = config.CenterName;
        _addressTextBox.Text = config.Address;
        _phoneTextBox.Text = config.Phone;
        _emailTextBox.Text = config.Email;
        _currencyTextBox.Text = config.DefaultCurrency;
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(_centerNameTextBox.Text))
        {
            MessageBox.Show("El nombre del centro es obligatorio.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!ValidationHelper.IsValidEmail(_emailTextBox.Text))
        {
            MessageBox.Show("El correo no tiene un formato válido.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var config = new CenterConfig
        {
            CenterName = _centerNameTextBox.Text.Trim(),
            Address = _addressTextBox.Text.Trim(),
            Phone = _phoneTextBox.Text.Trim(),
            Email = _emailTextBox.Text.Trim(),
            DefaultCurrency = string.IsNullOrWhiteSpace(_currencyTextBox.Text) ? "CLP" : _currencyTextBox.Text.Trim()
        };

        _repository.Save(config);
        _auditService.Log(_currentUser.Id, "Actualizar", "Configuración", "1", config.CenterName);
        MessageBox.Show("Configuración guardada correctamente.", "Configuración", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

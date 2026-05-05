using OftalmoLibre.Helpers;
using OftalmoLibre.Models;
using OftalmoLibre.Repositories;
using OftalmoLibre.Services;

namespace OftalmoLibre.Forms;

public sealed class PaymentForm : Form
{
    private readonly User _currentUser;
    private readonly PaymentRepository _repository = new();
    private readonly PatientRepository _patientRepository = new();
    private readonly AppointmentRepository _appointmentRepository = new();
    private readonly AttentionRepository _attentionRepository = new();
    private readonly AuditService _auditService = new();
    private readonly Payment _payment;

    private readonly ComboBox _patientCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _appointmentCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _attentionCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly DateTimePicker _datePicker = new() { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy HH:mm" };
    private readonly NumericUpDown _amountNumeric = new() { DecimalPlaces = 0, Maximum = 1000000000 };
    private readonly ComboBox _methodCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _referenceTextBox = new();
    private readonly TextBox _notesTextBox = new() { Multiline = true, Height = 90 };

    public PaymentForm(User currentUser, Payment? payment = null)
    {
        _currentUser = currentUser;
        _payment = payment ?? new Payment { PaymentDate = DateTime.Now, Method = "Efectivo" };

        Text = _payment.Id == 0 ? "Registrar pago" : "Editar pago";
        Width = 720;
        Height = 520;
        StartPosition = FormStartPosition.CenterParent;

        BuildLayout();
        LoadData();
    }

    private void BuildLayout()
    {
        var layout = UiHelper.CreateEditorLayout(8);
        UiHelper.AddLabeledControl(layout, "Paciente", _patientCombo, 0);
        UiHelper.AddLabeledControl(layout, "Cita asociada", _appointmentCombo, 1);
        UiHelper.AddLabeledControl(layout, "Atención asociada", _attentionCombo, 2);
        UiHelper.AddLabeledControl(layout, "Fecha de pago", _datePicker, 3);
        UiHelper.AddLabeledControl(layout, "Monto", _amountNumeric, 4);
        UiHelper.AddLabeledControl(layout, "Método", _methodCombo, 5);
        UiHelper.AddLabeledControl(layout, "Referencia", _referenceTextBox, 6);
        UiHelper.AddLabeledControl(layout, "Observaciones", _notesTextBox, 7);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 56, Padding = new Padding(12) };
        var saveButton = UiHelper.CreatePrimaryButton("Guardar", (_, _) => Save());
        var cancelButton = new Button { AutoSize = true, Text = "Cancelar" };
        cancelButton.Click += (_, _) => Close();
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);

        Controls.Add(layout);
        Controls.Add(buttons);
    }

    private void LoadData()
    {
        _patientCombo.DataSource = _patientRepository.GetActive();
        _patientCombo.DisplayMember = nameof(Patient.FullName);
        _patientCombo.ValueMember = nameof(Patient.Id);
        _patientCombo.SelectedIndexChanged += (_, _) => LoadRelatedEntities();

        _methodCombo.Items.AddRange(new object[] { "Efectivo", "Tarjeta", "Transferencia", "Otro" });

        if (_payment.Id > 0)
        {
            _patientCombo.SelectedValue = _payment.PatientId;
        }

        LoadRelatedEntities();

        if (_payment.Id > 0)
        {
            _appointmentCombo.SelectedValue = _payment.AppointmentId;
            _attentionCombo.SelectedValue = _payment.AttentionId;
            _datePicker.Value = _payment.PaymentDate;
            _amountNumeric.Value = _payment.Amount;
            _methodCombo.SelectedItem = _payment.Method;
            _referenceTextBox.Text = _payment.Reference;
            _notesTextBox.Text = _payment.Notes;
        }
        else
        {
            _methodCombo.SelectedItem = "Efectivo";
        }
    }

    private void LoadRelatedEntities()
    {
        var appointmentItems = new List<LookupItem> { new() { Id = null, Text = "(Sin cita asociada)" } };
        var attentionItems = new List<LookupItem> { new() { Id = null, Text = "(Sin atención asociada)" } };

        if (_patientCombo.SelectedValue is int patientId)
        {
            appointmentItems.AddRange(_appointmentRepository.GetByPatient(patientId).Select(x => new LookupItem { Id = x.Id, Text = x.Display }));
            attentionItems.AddRange(_attentionRepository.GetByPatientForLookup(patientId).Select(x => new LookupItem
            {
                Id = x.Id,
                Text = $"{x.VisitDate:g} - {x.ChiefComplaint}"
            }));
        }

        _appointmentCombo.DataSource = appointmentItems;
        _appointmentCombo.DisplayMember = nameof(LookupItem.Text);
        _appointmentCombo.ValueMember = nameof(LookupItem.Id);

        _attentionCombo.DataSource = attentionItems;
        _attentionCombo.DisplayMember = nameof(LookupItem.Text);
        _attentionCombo.ValueMember = nameof(LookupItem.Id);
    }

    private void Save()
    {
        if (_patientCombo.SelectedValue is null || _amountNumeric.Value <= 0 || _methodCombo.SelectedItem is null)
        {
            MessageBox.Show("Paciente, monto y método de pago son obligatorios.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var isNew = _payment.Id == 0;
        _payment.PatientId = Convert.ToInt32(_patientCombo.SelectedValue);
        _payment.AppointmentId = _appointmentCombo.SelectedValue as int?;
        _payment.AttentionId = _attentionCombo.SelectedValue as int?;
        _payment.PaymentDate = _datePicker.Value;
        _payment.Amount = _amountNumeric.Value;
        _payment.Method = _methodCombo.SelectedItem.ToString() ?? "Efectivo";
        _payment.Reference = _referenceTextBox.Text.Trim();
        _payment.Notes = _notesTextBox.Text.Trim();

        _repository.Save(_payment);
        _auditService.Log(_currentUser.Id, isNew ? "Crear" : "Actualizar", "Pago", _payment.Id.ToString(), _payment.Amount.ToString("N0"));

        DialogResult = DialogResult.OK;
        Close();
    }
}

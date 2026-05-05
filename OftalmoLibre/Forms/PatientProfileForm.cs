using OftalmoLibre.Helpers;
using OftalmoLibre.Repositories;

namespace OftalmoLibre.Forms;

public sealed class PatientProfileForm : Form
{
    private readonly int _patientId;
    private readonly PatientRepository _patientRepository = new();
    private readonly AppointmentRepository _appointmentRepository = new();
    private readonly AttentionRepository _attentionRepository = new();
    private readonly PrescriptionRepository _prescriptionRepository = new();
    private readonly ExamRepository _examRepository = new();
    private readonly DiagnosisRepository _diagnosisRepository = new();
    private readonly PaymentRepository _paymentRepository = new();

    public PatientProfileForm(int patientId)
    {
        _patientId = patientId;

        Text = "Ficha del cliente";
        Width = 1100;
        Height = 700;
        StartPosition = FormStartPosition.CenterParent;

        Load += (_, _) => BuildLayout();
    }

    private void BuildLayout()
    {
        var patient = _patientRepository.GetById(_patientId);
        if (patient is null)
        {
            Close();
            return;
        }

        var root = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterDistance = 180
        };

        var info = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 8
        };

        info.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        info.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddInfo(info, 0, "Ficha", patient.RecordNumber);
        AddInfo(info, 1, "Nombre", patient.FullName);
        AddInfo(info, 2, "RUT / DNI", patient.DocumentNumber ?? "-");
        AddInfo(info, 3, "Edad", patient.Age == 0 ? "-" : patient.Age.ToString());
        AddInfo(info, 4, "Teléfono", patient.Phone1 ?? "-");
        AddInfo(info, 5, "Correo", patient.Email ?? "-");
        AddInfo(info, 6, "Previsión", patient.Insurance ?? "-");
        AddInfo(info, 7, "Observaciones", patient.GeneralNotes ?? "-");

        root.Panel1.Controls.Add(info);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateGridTab("Atenciones", _attentionRepository.GetByPatient(_patientId)));
        tabs.TabPages.Add(CreateGridTab("Recetas", _prescriptionRepository.GetByPatient(_patientId)));
        root.Panel2.Controls.Add(tabs);

        Controls.Clear();
        Controls.Add(root);
    }

    private static void AddInfo(TableLayoutPanel layout, int row, string title, string value)
    {
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Padding = new Padding(0, 6, 0, 6),
            Text = title
        }, 0, row);

        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Padding = new Padding(0, 6, 0, 6),
            Text = value
        }, 1, row);
    }

    private static TabPage CreateGridTab(string title, object dataSource)
    {
        var grid = new DataGridView { Dock = DockStyle.Fill };
        UiHelper.ConfigureGrid(grid);
        grid.DataSource = dataSource;

        var page = new TabPage(title);
        page.Controls.Add(grid);
        return page;
    }
}

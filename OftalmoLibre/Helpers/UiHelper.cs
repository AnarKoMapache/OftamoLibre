namespace OftalmoLibre.Helpers;

public static class UiHelper
{
    // Paleta de colores centralizada
    public static readonly Color SidebarBg      = Color.FromArgb(30, 36, 46);
    public static readonly Color SidebarBtnIdle = Color.FromArgb(30, 36, 46);  // mismo fondo = invisible hasta hover
    public static readonly Color SidebarBtnHover= Color.FromArgb(50, 60, 76);
    public static readonly Color SidebarBtnActive= Color.FromArgb(6, 148, 162);
    public static readonly Color SidebarText    = Color.FromArgb(226, 232, 240);
    public static readonly Color SidebarMuted   = Color.FromArgb(148, 163, 184);
    public static readonly Color SidebarDivider = Color.FromArgb(55, 65, 81);

    public static readonly Color PrimaryBlue    = Color.FromArgb(37, 99, 235);
    public static readonly Color PrimaryHover   = Color.FromArgb(29, 78, 216);
    public static readonly Color SecondaryGray  = Color.FromArgb(241, 245, 249);
    public static readonly Color SecondaryHover = Color.FromArgb(226, 232, 240);
    public static readonly Color SecondaryText  = Color.FromArgb(51, 65, 85);

    public static readonly Color ContentBg      = Color.FromArgb(248, 250, 252);
    public static readonly Color AccentTeal     = Color.FromArgb(6, 148, 162);
    public static readonly Color BorderColor    = Color.FromArgb(220, 227, 235);
    public static readonly Color TextPrimary    = Color.FromArgb(15, 23, 42);
    public static readonly Color TextSecondary  = Color.FromArgb(100, 116, 139);

    // Wine-compatible: fuerza el pintado del fondo sin depender de VisualStyles
    public static void ForcePanelBackground(Panel panel)
    {
        var color = panel.BackColor;
        panel.Paint += (_, e) =>
        {
            using var b = new SolidBrush(color);
            e.Graphics.FillRectangle(b, panel.ClientRectangle);
        };
    }

    // Crea un Label que funciona visualmente sobre fondos oscuros en Wine
    public static Label CreateDarkLabel(string text, float fontSize = 9F,
        FontStyle style = FontStyle.Regular, Color? foreColor = null)
    {
        return new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", fontSize, style),
            ForeColor = foreColor ?? SidebarText,
            Text = text
        };
    }

    public static void ConfigureGrid(DataGridView grid)
    {
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = BorderColor;
        grid.MultiSelect = false;
        grid.ReadOnly = true;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.RowTemplate.Height = 34;

        grid.DefaultCellStyle.Font = new Font("Segoe UI", 9.5F);
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.DefaultCellStyle.SelectionBackColor = AccentTeal;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;

        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 251, 255);
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = AccentTeal;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(241, 245, 249);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = TextPrimary;
        grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        grid.EnableHeadersVisualStyles = false;
    }

    public static Button CreatePrimaryButton(string text, EventHandler onClick)
    {
        var btn = new Button
        {
            AutoSize = true,
            BackColor = PrimaryBlue,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.White,
            Height = 36,
            Margin = new Padding(0, 0, 8, 0),
            MinimumSize = new Size(90, 36),
            Padding = new Padding(12, 0, 12, 0),
            Text = text,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = PrimaryHover;
        btn.Click += onClick;
        return btn;
    }

    public static Button CreateSecondaryButton(string text, EventHandler onClick)
    {
        var btn = new Button
        {
            AutoSize = true,
            BackColor = SecondaryGray,
            FlatStyle = FlatStyle.Flat,
            ForeColor = SecondaryText,
            Height = 36,
            Margin = new Padding(0, 0, 8, 0),
            MinimumSize = new Size(80, 36),
            Padding = new Padding(12, 0, 12, 0),
            Text = text,
            UseVisualStyleBackColor = false
        };
        btn.FlatAppearance.BorderColor = BorderColor;
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.MouseOverBackColor = SecondaryHover;
        btn.Click += onClick;
        return btn;
    }

    public static FlowLayoutPanel CreateToolbar(params Control[] controls)
    {
        var panel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 0, 0, 10),
            WrapContents = false
        };
        panel.Controls.AddRange(controls);
        return panel;
    }

    public static TableLayoutPanel CreateEditorLayout(int rows)
    {
        var layout = new TableLayoutPanel
        {
            AutoScroll = true,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            RowCount = rows
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < rows; i++)
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return layout;
    }

    public static void AddLabeledControl(TableLayoutPanel layout, string label, Control control, int row)
    {
        var lbl = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = TextSecondary,
            Padding = new Padding(0, 12, 8, 0),
            Text = label
        };
        control.Dock = DockStyle.Top;
        control.Margin = new Padding(0, 4, 0, 10);
        layout.Controls.Add(lbl, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    public static Label CreateSectionTitle(string text)
    {
        return new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            ForeColor = TextPrimary,
            Padding = new Padding(0, 0, 0, 12),
            Text = text
        };
    }

    // Botones de acciones al pie de los diálogos
    // ── 2-column entry-form helpers ─────────────────────────────────────────

    // 4-column layout: [label | field | label | field]
    public static TableLayoutPanel Create2ColLayout()
    {
        var t = new TableLayoutPanel
        {
            AutoScroll = true,
            ColumnCount = 4,
            Dock = DockStyle.Fill,
            Padding = new Padding(18, 14, 18, 4)
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  50));
        return t;
    }

    // Adds one row with two label+field pairs
    public static void Add2Row(TableLayoutPanel t, int row,
        string lbl1, Control c1, string lbl2 = "", Control? c2 = null)
    {
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        c1.Dock   = DockStyle.Fill;
        c1.Margin = new Padding(0, 4, 14, 4);
        t.Controls.Add(FormLabel(lbl1), 0, row);
        t.Controls.Add(c1, 1, row);
        if (c2 is null || string.IsNullOrEmpty(lbl2)) return;
        c2.Dock   = DockStyle.Fill;
        c2.Margin = new Padding(0, 4, 0, 4);
        t.Controls.Add(FormLabel(lbl2), 2, row);
        t.Controls.Add(c2, 3, row);
    }

    // Adds one full-width row (field spans columns 1–3)
    public static void Add2RowFull(TableLayoutPanel t, int row, string lbl, Control c)
    {
        t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        c.Dock   = DockStyle.Fill;
        c.Margin = new Padding(0, 4, 0, 4);
        t.Controls.Add(FormLabel(lbl), 0, row);
        t.Controls.Add(c, 1, row);
        t.SetColumnSpan(c, 3);
    }

    private static Label FormLabel(string text) => new()
    {
        AutoSize  = true,
        Dock      = DockStyle.Fill,
        Font      = new Font("Segoe UI", 9F),
        ForeColor = TextSecondary,
        Padding   = new Padding(0, 8, 8, 0),
        Text      = text
    };

    // ── Dialog buttons ───────────────────────────────────────────────────────

    public static FlowLayoutPanel CreateDialogButtons(params Button[] buttons)
    {
        var panel = new FlowLayoutPanel
        {
            BackColor = Color.FromArgb(248, 250, 252),
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 60,
            Padding = new Padding(16, 12, 16, 12)
        };
        panel.Paint += (_, e) =>
        {
            using var pen = new Pen(BorderColor);
            e.Graphics.DrawLine(pen, 0, 0, panel.Width, 0);
        };
        panel.Controls.AddRange(buttons);
        return panel;
    }
}

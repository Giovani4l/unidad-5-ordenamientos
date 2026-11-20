namespace Laboratorio.Forms;

partial class MainForm
{
    private readonly Color tinta = Color.FromArgb(26, 43, 62);
    private readonly Color azul = Color.FromArgb(24, 89, 140);
    private readonly TextBox tamanos = new() { Text = "10000,50000,100000", Width = 183 };
    private readonly NumericUpDown semilla = new() { Maximum = int.MaxValue, Minimum = int.MinValue, Value = 2026, Width = 86 };
    private readonly Button ejecutar = new() { Text = "Ejecutar banco", AutoSize = true };
    private readonly Button cancelar = new() { Text = "Cancelar", AutoSize = true, Enabled = false };
    private readonly Button exportar = new() { Text = "Exportar CSV", AutoSize = true, Enabled = false };
    private readonly DataGridView tabla = new();
    private readonly Label estado = new() { AutoSize = true, Text = "Ajusta los datos y ejecuta el banco." };
    private readonly TabControl paginas = new() { Dock = DockStyle.Fill };
    private readonly NumericUpDown muestras = new() { Minimum = 1, Maximum = 5, Value = 3, Width = 52 };
    private readonly CheckedListBox metodos = new() { CheckOnClick = true, MultiColumn = true, ColumnWidth = 130, Width = 942, Height = 27 };
    private readonly ComboBox condicion = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 126 };
    private readonly ComboBox metrica = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 166 };
    private readonly Grafica grafica = new();

    private void InitializeComponent()
    {
        Iniciar("Banco de ordenamiento", "Siete métodos, tres órdenes de entrada y medición de CPU y reloj.", "Unidad 5");
        ConfigurarTabla(tabla, "Registros", "Entrada", "Método", "CPU (ms)", "Reloj (ms)", "Muestras", "Mínimo", "Máximo");
        tabla.Columns[2].FillWeight = 155;
        Pagina("Resultados", tabla,
            Fila(Rotulo("Tamaños"), tamanos, Rotulo("Semilla"), semilla, Rotulo("Muestras"), muestras, ejecutar, cancelar, exportar),
            Fila(metodos), Fila(estado),
            Fila(Rotulo("CPU 0 indica resolución insuficiente. Burbuja con 100000 datos puede tardar; puedes cancelar.")));
        Pagina("Gráfica", grafica, Fila(Rotulo("Entrada"), condicion, Rotulo("Métrica"), metrica),
            Fila(Rotulo("Eje vertical logarítmico. En CPU se omiten los ceros del temporizador.")));
        ResumeLayout(true);
    }

    private void Iniciar(string titulo, string descripcion, string unidad)
    {
        SuspendLayout();
        Text = titulo + " | " + unidad;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.White;
        ForeColor = tinta;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 675);
        ClientSize = new Size(1120, 710);
        var marco = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3 };
        marco.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        marco.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        marco.RowStyles.Add(new RowStyle(SizeType.Absolute, 29));
        var cabecera = new Panel { Dock = DockStyle.Fill, BackColor = tinta, Margin = Padding.Empty };
        cabecera.Controls.Add(new Label { Text = titulo, AutoSize = true, Font = new Font("Segoe UI", 21, FontStyle.Bold),
            ForeColor = Color.White, Location = new Point(20, 9) });
        cabecera.Controls.Add(new Label { Text = descripcion, AutoSize = true, ForeColor = Color.FromArgb(219, 231, 244),
            Location = new Point(22, 52) });
        marco.Controls.Add(cabecera, 0, 0);
        marco.Controls.Add(paginas, 0, 1);
        marco.Controls.Add(new Label { Text = "  Axel Giovani Gonzalez Lopez  |  I24050379  |  " + unidad + "  |  C# / .NET 10",
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = tinta }, 0, 2);
        Controls.Add(marco);
    }

    private TabPage Pagina(string titulo, Control cuerpo, params Control[] superiores)
    {
        var pagina = new TabPage(titulo) { BackColor = Color.White };
        var layout = new TableLayoutPanel { ColumnCount = 1, RowCount = superiores.Length + 1, Dock = DockStyle.Fill, Padding = new Padding(10) };
        for (int i = 0; i < superiores.Length; i++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.Controls.Add(superiores[i], 0, i);
        }
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        cuerpo.Dock = DockStyle.Fill;
        layout.Controls.Add(cuerpo, 0, superiores.Length);
        pagina.Controls.Add(layout);
        paginas.TabPages.Add(pagina);
        return pagina;
    }

    private static Label Rotulo(string texto) => new() { Text = texto, AutoSize = true, Margin = new Padding(5, 8, 4, 3) };
    private static FlowLayoutPanel Fila(params Control[] controles)
    {
        var fila = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            WrapContents = true, Padding = new Padding(0, 2, 0, 4) };
        fila.Controls.AddRange(controles);
        return fila;
    }

    private void ConfigurarTabla(DataGridView grid, params string[] columnas)
    {
        grid.BackgroundColor = Color.White;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.ReadOnly = true;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.EnableHeadersVisualStyles = false;
        grid.ColumnHeadersHeight = 38;
        grid.ColumnHeadersDefaultCellStyle.BackColor = tinta;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        grid.DefaultCellStyle.Padding = new Padding(3, 0, 3, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(239, 246, 252);
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        foreach (var texto in columnas)
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = texto, MinimumWidth = 45 });
        ejecutar.BackColor = azul;
        ejecutar.ForeColor = Color.White;
        ejecutar.FlatStyle = FlatStyle.Flat;
    }
}

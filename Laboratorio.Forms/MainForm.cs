using Algoritmos.Core;

namespace Laboratorio.Forms;

public partial class MainForm : Form
{
    private CancellationTokenSource? actual;
    private List<Medicion> filas = [];
    public MainForm()
    {
        InitializeComponent();
        foreach (Metodo m in Enum.GetValues<Metodo>()) metodos.Items.Add(m, true);
        condicion.Items.AddRange(Enum.GetNames<Condicion>());
        metrica.Items.AddRange(["CPU (ms)", "Reloj (ms)"]);
        condicion.SelectedIndex = metrica.SelectedIndex = 0;
        condicion.SelectedIndexChanged += (_, _) => ActualizarGrafica();
        metrica.SelectedIndexChanged += (_, _) => ActualizarGrafica();
        ejecutar.Click += async (_, _) => await Ejecutar();
        cancelar.Click += (_, _) => actual?.Cancel();
        exportar.Click += (_, _) => Guardar();
        FormClosing += (_, e) =>
        {
            if (actual is null) return;
            actual.Cancel(); e.Cancel = true;
            estado.Text = "Cancelando. Cierra la ventana cuando termine.";
        };
    }

    private async Task Ejecutar()
    {
        if (actual is not null) return;
        actual = new();
        ejecutar.Enabled = exportar.Enabled = false;
        cancelar.Enabled = true;
        filas = [];
        tabla.Rows.Clear();
        ActualizarGrafica();
        estado.Text = "Ejecutando y verificando cada salida...";
        try
        {
            int[] ns = tamanos.Text.Split(',').Select(s => int.Parse(s.Trim())).ToArray();
            int s = (int)semilla.Value, r = (int)muestras.Value;
            var ms = metodos.CheckedItems.Cast<Metodo>().ToArray();
            void Mostrar(Medicion m) => Invoke(() =>
            {
                tabla.Rows.Add(m.N, m.Entrada, m.Metodo, m.CpuMs, m.RelojMs, m.Muestras, m.Primera, m.Ultima);
                estado.Text = $"{tabla.Rows.Count} combinaciones verificadas...";
            });
            var token = actual.Token;
            filas = await Task.Run(() => Banco.Ejecutar(ns, s, r, ms, Mostrar, token));
            ActualizarGrafica();
            exportar.Enabled = filas.Count > 0;
            estado.Text = $"{filas.Count} combinaciones verificadas. Puedes abrir la gráfica o exportar los resultados.";
        }
        catch (OperationCanceledException) { estado.Text = "Banco cancelado. Las filas parciales visibles no se exportan."; }
        catch (Exception ex) { estado.Text = "No se completó: " + ex.Message; }
        finally { actual.Dispose(); actual = null; cancelar.Enabled = false; ejecutar.Enabled = true; }
    }

    private void ActualizarGrafica() => grafica.Mostrar(filas, (Condicion)Math.Max(0, condicion.SelectedIndex), metrica.SelectedIndex == 0);

    private void Guardar()
    {
        using var dialogo = new SaveFileDialog { Filter = "Archivo CSV|*.csv", FileName = "rendimiento.csv" };
        if (dialogo.ShowDialog(this) != DialogResult.OK) return;
        try { Banco.Exportar(dialogo.FileName, filas); estado.Text = "CSV guardado."; }
        catch (IOException ex) { estado.Text = ex.Message; }
        catch (UnauthorizedAccessException ex) { estado.Text = ex.Message; }
    }
}

using Algoritmos.Core;
using System.Drawing.Drawing2D;

namespace Laboratorio.Forms;

internal sealed class Grafica : Control
{
    private static readonly Color[] Colores = [Color.Firebrick, Color.RoyalBlue, Color.DarkGreen,
        Color.DarkViolet, Color.DarkOrange, Color.Teal, Color.SaddleBrown];
    private IReadOnlyList<Medicion> filas = [];
    private Condicion condicion;
    private bool cpu;

    public Grafica() { DoubleBuffered = true; ResizeRedraw = true; BackColor = Color.White; }
    public void Mostrar(IReadOnlyList<Medicion> datos, Condicion orden, bool cpu)
    { filas = datos; condicion = orden; this.cpu = cpu; Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        double Valor(Medicion r) => cpu ? r.CpuMs : r.RelojMs;
        var puntos = filas.Where(r => r.Entrada == condicion && Valor(r) > 0).ToArray();
        if (puntos.Length == 0)
        {
            g.DrawString("Sin valores positivos para graficar. Ejecuta el banco o cambia a Reloj.",
                Font, Brushes.Black, 25, 25);
            return;
        }
        var tamanos = filas.Select(r => r.N).Distinct().Order().ToArray();
        int abajo = Height - 59, izquierda = 76, derecha = Width - 200, arriba = 35;
        if (abajo <= arriba || derecha <= izquierda) return;
        double menor = Math.Floor(Math.Log10(puntos.Min(Valor)));
        double mayor = Math.Max(menor + 1, Math.Ceiling(Math.Log10(puntos.Max(Valor))));
        float X(int n) => tamanos.Length == 1 ? (izquierda + derecha) / 2f :
            izquierda + (derecha - izquierda) * Array.IndexOf(tamanos, n) / (float)(tamanos.Length - 1);
        float Y(double v) => (float)(abajo - (abajo - arriba) * (Math.Log10(v) - menor) / (mayor - menor));
        g.DrawString(cpu ? "Mediana de CPU (ms)" : "Mediana de reloj (ms)", Font, Brushes.Black, 8, 5);
        for (double i = menor; i <= mayor; i++)
        {
            double y = Math.Pow(10, i);
            g.DrawLine(Pens.Gainsboro, izquierda, Y(y), derecha, Y(y));
            g.DrawString(y.ToString("0.######"), Font, Brushes.Black, 8, Y(y) - 9);
        }
        g.DrawLine(Pens.Black, izquierda, arriba, izquierda, abajo);
        g.DrawLine(Pens.Black, izquierda, abajo, derecha, abajo);
        foreach (int n in tamanos) g.DrawString(n.ToString(), Font, Brushes.Black, X(n) - 20, abajo + 9);
        g.DrawString("Registros (N)", Font, Brushes.Black, (izquierda + derecha) / 2f - 35, abajo + 32);
        int leyenda = arriba;
        foreach (var grupo in puntos.GroupBy(r => r.Metodo).OrderBy(g => g.Key))
        {
            var color = Colores[(int)grupo.Key];
            using var lapiz = new Pen(color, 2);
            using var pincel = new SolidBrush(color);
            var linea = grupo.OrderBy(r => r.N).Select(r => new PointF(X(r.N), Y(Valor(r)))).ToArray();
            if (linea.Length > 1) g.DrawLines(lapiz, linea);
            foreach (var p in linea) g.FillEllipse(pincel, p.X - 4, p.Y - 4, 8, 8);
            g.DrawLine(lapiz, derecha + 16, leyenda + 9, derecha + 39, leyenda + 9);
            g.DrawString(grupo.Key.ToString(), Font, Brushes.Black, derecha + 46, leyenda);
            leyenda += 25;
        }
    }
}

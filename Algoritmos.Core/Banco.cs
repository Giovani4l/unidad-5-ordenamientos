using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Algoritmos.Core;

public enum Condicion { Aleatorio, Ordenado, Inverso }
public sealed record Medicion(int N, Condicion Entrada, Metodo Metodo, double CpuMs, double RelojMs,
    int Muestras, int Primera, int Ultima);

public static class Banco
{
    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentThread();
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetThreadTimes(IntPtr hilo, out long creado, out long salida,
        out long kernel, out long usuario);

    private static long Cpu()
    {
        if (!GetThreadTimes(GetCurrentThread(), out _, out _, out long kernel, out long usuario))
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        return kernel + usuario;
    }

    public static bool Estable(Metodo m) => m is not (Metodo.Quicksort or Metodo.ShellSort);
    public static bool Externo(Metodo m) => m >= Metodo.Intercalacion;

    public static void Interno(Metodo metodo, Registro[] datos, CancellationToken token)
    {
        switch (metodo)
        {
            case Metodo.Burbuja: Ordenamientos.Burbuja(datos, token); break;
            case Metodo.Quicksort: Ordenamientos.Quicksort(datos, token); break;
            case Metodo.ShellSort: Ordenamientos.ShellSort(datos, token); break;
            case Metodo.Radix: Ordenamientos.Radix(datos, token); break;
            default: throw new ArgumentException("Se esperaba un método interno.");
        }
    }

    public static string Externo(Metodo metodo, string origen, string carpeta, CancellationToken token) =>
        metodo switch
        {
            Metodo.Intercalacion => OrdenExterno.Intercalacion(origen, carpeta, token),
            Metodo.MezclaDirecta => OrdenExterno.MezclaDirecta(origen, carpeta, token),
            Metodo.MezclaNatural => OrdenExterno.MezclaNatural(origen, carpeta, token),
            _ => throw new ArgumentException("Se esperaba un método externo.")
        };

    public static List<Medicion> Ejecutar(int[] tamanos, int semilla, int muestras, Metodo[] metodos,
        Action<Medicion>? mostrar, CancellationToken token)
    {
        if (tamanos.Length is < 1 or > 6 || tamanos.Any(n => n is < 1 or > 100000) ||
            muestras is < 1 or > 5 || metodos.Length == 0)
            throw new ArgumentException("Usa de 1 a 6 tamaños entre 1 y 100000, de 1 a 5 muestras y algún método.");

        // Un calentamiento pequeño por método, separado de la tabla cronometrada.
        var calentamiento = CrearDatos(256, semilla, Condicion.Aleatorio);
        var referencia = Referencia(calentamiento);
        foreach (var metodo in metodos) Una(metodo, calentamiento, referencia, token);
        var resultados = new List<Medicion>();
        foreach (int n in tamanos.Distinct())
        foreach (Condicion entrada in Enum.GetValues<Condicion>())
        {
            var datos = CrearDatos(n, semilla, entrada);
            var esperado = Referencia(datos);
            foreach (var metodo in metodos)
            {
                var cpu = new double[muestras];
                var reloj = new double[muestras];
                for (int i = 0; i < muestras; i++)
                    (cpu[i], reloj[i]) = Una(metodo, datos, esperado, token);
                Array.Sort(cpu); Array.Sort(reloj);
                double Mediana(double[] a) => (a[(a.Length - 1) / 2] + a[a.Length / 2]) / 2;
                var fila = new Medicion(n, entrada, metodo, Mediana(cpu), Mediana(reloj),
                    muestras, esperado[0], esperado[^1]);
                resultados.Add(fila);
                mostrar?.Invoke(fila);
            }
        }
        return resultados;
    }

    public static Registro[] CrearDatos(int n, int semilla, Condicion orden)
    {
        var azar = new Random(unchecked(semilla + n));
        var claves = new int[n];
        for (int i = 0; i < n; i++) claves[i] = azar.Next(-10 * n, 10 * n + 1);
        if (orden != Condicion.Aleatorio) Array.Sort(claves);
        if (orden == Condicion.Inverso) Array.Reverse(claves);
        return claves.Select((k, i) => new Registro(k, i)).ToArray();
    }

    private static int[] Referencia(Registro[] datos)
    {
        var claves = Array.ConvertAll(datos, r => r.Clave);
        Array.Sort(claves);
        return claves;
    }

    private static (double Cpu, double Reloj) Una(Metodo metodo, Registro[] datos, int[] esperado,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (!Externo(metodo))
        {
            var copia = (Registro[])datos.Clone();
            long t = Stopwatch.GetTimestamp(), c = Cpu();
            Interno(metodo, copia, token);
            double cpu = (Cpu() - c) / 10000d, reloj = Stopwatch.GetElapsedTime(t).TotalMilliseconds;
            Verificar(metodo, datos, esperado, copia);
            return (cpu, reloj);
        }
        string dir = Directory.CreateTempSubdirectory("unidad5-").FullName;
        try
        {
            string origen = Path.Combine(dir, "entrada.bin");
            OrdenExterno.Guardar(origen, datos);
            long t = Stopwatch.GetTimestamp(), c = Cpu();
            string resultado = Externo(metodo, origen, dir, token);
            double cpu = (Cpu() - c) / 10000d, reloj = Stopwatch.GetElapsedTime(t).TotalMilliseconds;
            Verificar(metodo, datos, esperado, OrdenExterno.LeerArchivo(resultado));
            return (cpu, reloj);
        }
        finally { Directory.Delete(dir, true); }
    }

    private static void Verificar(Metodo metodo, Registro[] original, int[] esperado, IEnumerable<Registro> salida)
    {
        int i = 0, anterior = -1;
        var vistos = new bool[original.Length];
        foreach (var r in salida)
        {
            if (i >= esperado.Length || r.Clave != esperado[i] || (uint)r.OrdenOriginal >= (uint)vistos.Length ||
                vistos[r.OrdenOriginal] || original[r.OrdenOriginal] != r ||
                (Estable(metodo) && i > 0 && esperado[i - 1] == r.Clave && anterior > r.OrdenOriginal))
                throw new InvalidOperationException($"Salida incorrecta de {metodo} en el índice {i}.");
            vistos[r.OrdenOriginal] = true;
            anterior = r.OrdenOriginal;
            i++;
        }
        if (i != esperado.Length) throw new InvalidOperationException($"Longitud incorrecta de {metodo}.");
    }

    public static void Exportar(string ruta, IEnumerable<Medicion> filas)
    {
        using var salida = new StreamWriter(ruta);
        salida.WriteLine("n,condicion,metodo,cpu_ms,reloj_ms,muestras,minimo,maximo");
        foreach (var r in filas)
            salida.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"{r.N},{r.Entrada},{r.Metodo},{r.CpuMs:F4},{r.RelojMs:F4},{r.Muestras},{r.Primera},{r.Ultima}"));
    }
}

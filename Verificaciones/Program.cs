using Algoritmos.Core;
using System.Diagnostics;
using System.Runtime.InteropServices;

string carpeta = args.Length == 0 ? "evidencias" : args[0];
Directory.CreateDirectory(carpeta);
int verificados = 0;
int[][] casosBase = [[], [0], [int.MaxValue, 0, int.MinValue, -1, int.MaxValue],
    [4, 3, 2, 1], [1, 2, 3, 4], [7, 7, 7, 7]];
var casos = casosBase.ToList();
var azar = new Random(2026);
for (int i = 0; i < 32; i++)
    casos.Add(Enumerable.Range(0, azar.Next(1, 180)).Select(_ => azar.Next(-25, 26)).ToArray());
casos.Add(Enumerable.Range(0, 4097).Select(_ => azar.Next(-1000, 1001)).ToArray());
casos.Add(Enumerable.Range(0, 33000).Select(_ => azar.Next(-500, 501)).ToArray());
foreach (var caso in casos)
foreach (Metodo metodo in Enum.GetValues<Metodo>())
{
    var datos = caso.Select((x, i) => new Registro(x, i)).ToArray();
    var esperado = datos.OrderBy(x => x.Clave).ToArray(); // Oráculo estable.
    Registro[] salida;
    if (Banco.Externo(metodo))
    {
        string dir = Directory.CreateTempSubdirectory("u5-validar-").FullName;
        try
        {
            string origen = Path.Combine(dir, "in.bin");
            OrdenExterno.Guardar(origen, datos);
            salida = OrdenExterno.LeerArchivo(Banco.Externo(metodo, origen, dir, default)).ToArray();
        }
        finally { Directory.Delete(dir, true); }
    }
    else
    {
        salida = (Registro[])datos.Clone();
        Banco.Interno(metodo, salida, default);
    }
    if (!salida.Select(x => x.Clave).SequenceEqual(esperado.Select(x => x.Clave)) ||
        !salida.OrderBy(x => x.OrdenOriginal).SequenceEqual(datos) ||
        (Banco.Estable(metodo) && !salida.SequenceEqual(esperado)))
        throw new Exception($"Fallo de {metodo} con {caso.Length} registros.");
    verificados++;
}
Console.WriteLine($"Verificados {verificados} pares método/entrada: orden, conservación y estabilidad donde aplica.");
Console.WriteLine($"{RuntimeInformation.OSDescription} | {RuntimeInformation.FrameworkDescription} | {RuntimeInformation.ProcessArchitecture}");
if (args.Contains("--solo-pruebas")) return;
var reloj = Stopwatch.StartNew();
var resultados = Banco.Ejecutar([10000, 50000, 100000], 2026, 3, Enum.GetValues<Metodo>(),
    r => Console.WriteLine($"{r.N,6}  {r.Entrada,-10} {r.Metodo,-15} CPU={r.CpuMs,10:F4} ms; reloj={r.RelojMs,10:F4} ms"), default);
Banco.Exportar(Path.Combine(carpeta, "rendimiento.csv"), resultados);
Console.WriteLine($"{resultados.Count} combinaciones, cada una validada en sus 3 muestras. Duración del banco: {reloj.Elapsed.TotalSeconds:F2} s.");

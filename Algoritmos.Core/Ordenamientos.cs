namespace Algoritmos.Core;

public readonly record struct Registro(int Clave, int OrdenOriginal);
public enum Metodo { Burbuja, Quicksort, ShellSort, Radix, Intercalacion, MezclaDirecta, MezclaNatural }

public static class Ordenamientos
{
    public static void Burbuja(Registro[] a, CancellationToken token = default)
    {
        for (int fin = a.Length - 1; fin > 0; fin--)
        {
            token.ThrowIfCancellationRequested();
            bool cambio = false;
            for (int i = 0; i < fin; i++)
            {
                if (a[i].Clave <= a[i + 1].Clave) continue;
                (a[i], a[i + 1]) = (a[i + 1], a[i]);
                cambio = true;
            }
            if (!cambio) break;
        }
    }

    public static void Quicksort(Registro[] a, CancellationToken token = default)
    {
        void Ordenar(int inicio, int fin)
        {
            while (inicio < fin)
            {
                token.ThrowIfCancellationRequested();
                // Tres grupos: menores, iguales y mayores al pivote central.
                int pivote = a[inicio + (fin - inicio) / 2].Clave;
                int izq = inicio, i = inicio, der = fin;
                while (i <= der)
                {
                    if (a[i].Clave < pivote) { (a[i], a[izq]) = (a[izq], a[i]); i++; izq++; }
                    else if (a[i].Clave > pivote) { (a[i], a[der]) = (a[der], a[i]); der--; }
                    else i++;
                }
                // Solo recurre en el lado menor. El otro se procesa con el ciclo.
                if (izq - inicio < fin - der) { Ordenar(inicio, izq - 1); inicio = der + 1; }
                else { Ordenar(der + 1, fin); fin = izq - 1; }
            }
        }
        Ordenar(0, a.Length - 1);
    }

    public static void ShellSort(Registro[] a, CancellationToken token = default)
    {
        int salto = 1;
        while (salto < a.Length / 3) salto = 3 * salto + 1;
        for (; salto >= 1; salto /= 3)
        {
            token.ThrowIfCancellationRequested();
            for (int i = salto; i < a.Length; i++)
            {
                Registro actual = a[i];
                int j = i;
                while (j >= salto && a[j - salto].Clave > actual.Clave)
                { a[j] = a[j - salto]; j -= salto; }
                a[j] = actual;
            }
        }
    }

    public static void Radix(Registro[] a, CancellationToken token = default)
    {
        if (a.Length < 2) return;
        var auxiliar = new Registro[a.Length];
        Span<int> posiciones = stackalloc int[256];
        Registro[] entrada = a, salida = auxiliar;
        for (int bits = 0; bits < 32; bits += 8)
        {
            token.ThrowIfCancellationRequested();
            posiciones.Clear();
            foreach (var r in entrada) posiciones[Digito(r.Clave, bits)]++;
            int acumulado = 0;
            for (int i = 0; i < posiciones.Length; i++)
            { int cantidad = posiciones[i]; posiciones[i] = acumulado; acumulado += cantidad; }
            foreach (var r in entrada) salida[posiciones[Digito(r.Clave, bits)]++] = r;
            (entrada, salida) = (salida, entrada);
        }
        // Cuatro pasadas alternadas dejan el resultado en el arreglo original.
    }

    private static int Digito(int clave, int bits) =>
        (int)(((unchecked((uint)clave) ^ 0x80000000u) >> bits) & 255u);
}

namespace Algoritmos.Core;

/// <summary>Registros binarios de dos Int32; la etiqueta -1 delimita corridas temporales.</summary>
public static class OrdenExterno
{
    public const int TamanoBloque = 4096;
    public const int Abanico = 8;
    private const int Buffer = 16 * 1024;
    private static BinaryReader Abrir(string ruta) =>
        new(new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.Read, Buffer));
    private static BinaryWriter Crear(string ruta) =>
        new(new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None, Buffer));

    private static bool Leer(BinaryReader lector, out Registro registro)
    {
        if (lector.BaseStream.Position == lector.BaseStream.Length) { registro = default; return false; }
        registro = new(lector.ReadInt32(), lector.ReadInt32());
        return true;
    }

    private static void Escribir(BinaryWriter salida, Registro r)
    { salida.Write(r.Clave); salida.Write(r.OrdenOriginal); }

    public static void Guardar(string ruta, IEnumerable<Registro> registros)
    {
        using var salida = Crear(ruta);
        foreach (var r in registros) Escribir(salida, r);
    }

    public static IEnumerable<Registro> LeerArchivo(string ruta)
    {
        using var lector = Abrir(ruta);
        while (Leer(lector, out var r)) yield return r;
    }

    /// <summary>Construye bloques ordenados estables y los intercala de hasta ocho en ocho.</summary>
    public static string Intercalacion(string origen, string carpeta, CancellationToken token)
    {
        int corridas = 0;
        string Ruta(int nivel, int corrida) => Path.Combine(carpeta, $"i{nivel}_{corrida}.bin");
        using (var lector = Abrir(origen))
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                var bloque = new Registro[TamanoBloque];
                int n = 0;
                while (n < bloque.Length && Leer(lector, out bloque[n])) n++;
                if (n == 0) break;
                if (n < bloque.Length) Array.Resize(ref bloque, n);
                Ordenamientos.Radix(bloque, token);
                Guardar(Ruta(0, corridas++), bloque);
            }
        }
        if (corridas == 0) return origen;
        int pasada = 0;
        while (corridas > 1)
        {
            int creadas = 0;
            for (int i = 0; i < corridas; i += Abanico)
            {
                token.ThrowIfCancellationRequested();
                var entradas = Enumerable.Range(i, Math.Min(Abanico, corridas - i))
                    .Select(k => Ruta(pasada, k)).ToArray();
                FusionarGrupo(entradas, Ruta(pasada + 1, creadas++), token);
                foreach (var entrada in entradas) File.Delete(entrada);
            }
            pasada++;
            corridas = creadas;
        }
        return Ruta(pasada, 0);
    }

    private static void FusionarGrupo(string[] rutas, string destino, CancellationToken token)
    {
        var lectores = new BinaryReader[rutas.Length];
        var cola = new PriorityQueue<(Registro Valor, int Corrida), (int Clave, int Corrida)>();
        using var salida = Crear(destino);
        try
        {
            for (int i = 0; i < rutas.Length; i++)
            {
                var lector = lectores[i] = Abrir(rutas[i]);
                if (Leer(lector, out var r)) cola.Enqueue((r, i), (r.Clave, i));
            }
            int cantidad = 0;
            while (cola.TryDequeue(out var entrada, out _))
            {
                if ((cantidad++ & 4095) == 0) token.ThrowIfCancellationRequested();
                Escribir(salida, entrada.Valor);
                if (Leer(lectores[entrada.Corrida], out var r))
                    cola.Enqueue((r, entrada.Corrida), (r.Clave, entrada.Corrida));
            }
        }
        finally { foreach (var lector in lectores) lector?.Dispose(); }
    }

    public static string MezclaDirecta(string origen, string carpeta, CancellationToken token) =>
        MezclarPasadas(origen, carpeta, false, token);

    public static string MezclaNatural(string origen, string carpeta, CancellationToken token) =>
        MezclarPasadas(origen, carpeta, true, token);

    private static string MezclarPasadas(string origen, string carpeta, bool natural, CancellationToken token)
    {
        int ancho = 1;
        string a = Path.Combine(carpeta, "a.bin"), b = Path.Combine(carpeta, "b.bin");
        string actual = origen;
        int pasada = 0;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            int corridas = Distribuir(actual, a, b, ancho, natural, token);
            if (corridas <= 1) return actual;
            string nuevo = Path.Combine(carpeta, $"m{pasada++ % 2}.bin");
            FusionarPares(a, b, nuevo, token);
            actual = nuevo;
            if (!natural) ancho = checked(ancho * 2);
        }
    }

    private static int Distribuir(string ruta, string a, string b, int ancho, bool natural,
        CancellationToken token)
    {
        using var entrada = Abrir(ruta);
        using var izquierda = Crear(a);
        using var derecha = Crear(b);
        var salida = izquierda;
        int corridas = 0, longitud = 0, anterior = 0, vistos = 0;
        while (Leer(entrada, out var r))
        {
            if ((vistos++ & 4095) == 0) token.ThrowIfCancellationRequested();
            if (longitud != 0 && (natural ? anterior > r.Clave : longitud == ancho))
            {
                Escribir(salida, new(0, -1));
                corridas++;
                salida = (corridas & 1) == 0 ? izquierda : derecha;
                longitud = 0;
            }
            Escribir(salida, r);
            anterior = r.Clave;
            longitud++;
        }
        if (longitud == 0) return corridas;
        Escribir(salida, new(0, -1));
        return corridas + 1;
    }

    private static void FusionarPares(string a, string b, string destino, CancellationToken token)
    {
        using var izquierda = Abrir(a);
        using var derecha = Abrir(b);
        using var salida = Crear(destino);
        bool aHay = Leer(izquierda, out var x), bHay = Leer(derecha, out var y);
        int escritos = 0;
        while (aHay || bHay)
        {
            bool aDato = aHay && x.OrdenOriginal >= 0, bDato = bHay && y.OrdenOriginal >= 0;
            if (!aDato && !bDato)
            {
                if (aHay) aHay = Leer(izquierda, out x);
                if (bHay) bHay = Leer(derecha, out y);
                continue;
            }
            if ((escritos++ & 4095) == 0) token.ThrowIfCancellationRequested();
            // En igualdad gana la corrida anterior para mantener la estabilidad.
            if (aDato && (!bDato || x.Clave <= y.Clave))
            { Escribir(salida, x); aHay = Leer(izquierda, out x); }
            else { Escribir(salida, y); bHay = Leer(derecha, out y); }
        }
    }
}

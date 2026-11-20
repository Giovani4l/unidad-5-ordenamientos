# Unidad 5 · Métodos de ordenamiento

Axel Giovani Gonzalez Lopez · I24050379 · Docente Ruben Riojas

## Abrir y ejecutar

Abre **Unidad5.sln** en Visual Studio compatible con .NET 10, elige
**Laboratorio.Forms** como proyecto de inicio y presiona F5. El diseño está en
`Laboratorio.Forms/MainForm.Designer.cs`. El ZIP incluye además
`Ejecutar/Laboratorio.Forms.exe`. Conserva los archivos que acompañan al ejecutable.
Se requiere Windows con .NET Desktop Runtime 10 y, para compilar, el SDK .NET 10.

1. En **Resultados** elige tamaños, semilla y muestras. Los tamaños se separan
   por comas. Se aceptan hasta seis tamaños, entre 1 y 100000.
2. Marca los métodos y pulsa **Ejecutar banco**. Cada combinación valida todas
   sus salidas. Burbuja con 100000 valores puede tardar varios segundos por
   muestra. El botón **Cancelar** detiene el banco en un punto seguro.
3. Usa **Gráfica** para elegir condición y métrica. CPU 0 significa que el
   temporizador no resolvió una mediana positiva. Para medir ejecuciones cortas,
   revisa la columna de tiempo transcurrido.
4. **Exportar CSV** guarda la última ejecución completa. Las columnas numéricas
   usan punto decimal.

## Código y reproducción

- `Ordenamientos.cs` contiene Burbuja, Quicksort, ShellSort y Radix.
- `OrdenExterno.cs` contiene intercalación por bloques de 4096 registros y
  hasta 8 vías, mezcla directa y mezcla natural. Cada método usa archivos binarios
  temporales con búfer fijo.
- `Banco.cs` prepara los datos, mide reloj y CPU por hilo, verifica conservación y
  estabilidad e incluye la exportación CSV. Los archivos temporales se eliminan
  al terminar cada muestra, incluso si se cancela.

```powershell
dotnet build Unidad5.sln -c Release -m:1
dotnet run --project Laboratorio.Forms -c Release
dotnet run --project Verificaciones -c Release -- evidencias_nuevas
```

El verificador comprueba 280 pares método/entrada. Después usa semilla 2026,
tres muestras y los tamaños 10000, 50000 y 100000 para exportar 63 filas.
`--solo-pruebas` después de la ruta de evidencias omite el banco completo.
`evidencias/rendimiento.csv` contiene la ejecución que alimenta el reporte.
Los PNG son renders de los formularios ejecutados mediante DrawToBitmap.

Publicación de GitHub y enlaces públicos de YouTube pendientes. Cuando publiques
el código, conserva este README y los CSV de evidencia. El guion literal está
en el paquete general de la unidad.

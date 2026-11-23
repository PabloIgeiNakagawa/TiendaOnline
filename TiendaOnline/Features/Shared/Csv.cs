namespace TiendaOnline.Features.Shared
{
    /// <summary>
    /// Armado de celdas para los CSV del panel: separador ";", coma decimal y
    /// fechas fijas a es-AR (no dependen de la cultura del server).
    /// </summary>
    internal static class Csv
    {
        // Celda con separador, comillas o salto de línea va entre comillas
        // (doblando las comillas internas), el resto sale pelado.
        public static string Celda(string valor)
        {
            if (valor.Contains(';') || valor.Contains('"') || valor.Contains('\n') || valor.Contains('\r'))
            {
                return $"\"{valor.Replace("\"", "\"\"")}\"";
            }

            return valor;
        }
    }
}

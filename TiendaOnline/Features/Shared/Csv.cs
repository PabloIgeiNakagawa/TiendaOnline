using System.Text;

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

        // Excel evalúa como fórmula toda celda de texto que arranca con =, +, -
        // o @: un nombre de cliente o producto "=HYPERLINK(...)" se ejecutaría
        // en la máquina del admin al abrir el CSV. El apóstrofo inicial es el
        // marcador de texto de Excel y no se muestra en la planilla. Solo para
        // texto libre: un número negativo ("-5,5") es un dato legítimo y debe
        // seguir siéndolo.
        public static string NeutralizarFormula(string valor)
        {
            if (valor.Length > 0 && valor[0] is '=' or '+' or '-' or '@' or '\t' or '\r')
            {
                return "'" + valor;
            }

            return valor;
        }

        // Archivo con BOM UTF-8: sin él Excel adivina ANSI y los acentos salen
        // rotos. GetBytes no emite el preamble aunque el encoding se cree con
        // encoderShouldEmitUTF8Identifier: se antepone a mano.
        public static byte[] Archivo(string texto)
        {
            var bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble();
            var contenido = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(texto);
            var bytes = new byte[bom.Length + contenido.Length];
            bom.CopyTo(bytes, 0);
            contenido.CopyTo(bytes, bom.Length);
            return bytes;
        }
    }
}


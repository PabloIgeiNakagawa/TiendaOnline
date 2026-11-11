namespace TiendaOnline.Application.Productos.Commands
{
    /// <summary>
    /// Archivo de imagen extra que viaja del controller al command service:
    /// stream abierto + nombre original (mismo contrato que la principal).
    /// </summary>
    public class ArchivoImagenDto
    {
        public Stream Stream { get; set; }
        public string NombreArchivo { get; set; }
    }
}

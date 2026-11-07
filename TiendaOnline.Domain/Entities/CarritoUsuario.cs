using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    /// <summary>
    /// Carrito persistido de un usuario logueado: una fila por usuario con el snapshot
    /// JSON de los items (mismo contenido que la session). Sobrevive la expiración de
    /// sesión y el cierre del browser; el precio/stock real se recalcula contra BD en
    /// cada lectura (ValidarStockAsync), acá solo importan ProductoId y Cantidad.
    /// </summary>
    public class CarritoUsuario
    {
        [Key]
        public int UsuarioId { get; set; }

        [Required]
        public string Contenido { get; set; }

        public DateTime FechaActualizacion { get; set; }
    }
}

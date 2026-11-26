using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    /// <summary>
    /// Última posición conocida del repartidor mientras comparte ubicación.
    /// Una fila por repartidor (el PK es el usuario): no se guarda historial,
    /// es un dato efímero de la entrega en curso.
    /// </summary>
    public class UbicacionRepartidor
    {
        [Key]
        public int UsuarioId { get; set; }

        public Usuario Usuario { get; set; }

        public double Latitud { get; set; }

        public double Longitud { get; set; }

        /// <summary>Precisión en metros que informa el GPS del teléfono.</summary>
        public double? PrecisionMetros { get; set; }

        public DateTime ActualizadoEn { get; set; } = DateTime.Now;
    }
}

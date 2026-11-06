using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaOnline.Domain.Entities
{
    /// <summary>
    /// Tarifa de envío a domicilio por provincia. La tabla arranca sembrada con las
    /// 24 provincias (mismo nombre que devuelve la API georef, fuente del select del
    /// formulario); el panel de envíos edita el costo y permite deshabilitar una zona.
    /// </summary>
    public class ZonaEnvio
    {
        [Key]
        public int ZonaEnvioId { get; set; }

        [Required]
        [StringLength(60)]
        public string Provincia { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Costo { get; set; }

        /// <summary>Deshabilitada = no despachamos a esa provincia (el checkout lo bloquea).</summary>
        public bool Activa { get; set; } = true;
    }
}

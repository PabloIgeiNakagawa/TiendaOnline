using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class TipoMovimientoStock
    {
        [Key]
        public int TipoMovimientoStockId { get; set; }

        [Required, MaxLength(50)]
        public string Nombre { get; set; }

        public ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();
    }
}

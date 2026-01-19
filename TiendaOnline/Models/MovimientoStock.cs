using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class MovimientoStock
    {
        [Key]
        public int MovimientoStockId { get; set; }

        [Required]
        public int StockId { get; set; }
        public Stock Stock { get; set; }

        [Required]
        public int TipoMovimientoStockId { get; set; }
        public TipoMovimientoStock TipoMovimientoStock { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public string? Observacion { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Stock
    {
        [Key]
        public int StockId { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        [Required]
        public int CantidadActual { get; set; }

        public ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();
    }
}

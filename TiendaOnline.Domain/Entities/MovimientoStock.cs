using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public enum TipoMovimiento
    {
        EntradaStock = 1,    // Compras a proveedores
        SalidaVenta = 2,     // Ventas a clientes
        Devolucion = 3,      // Cliente devuelve producto
        AjusteManual = 4,    // Corrección por rotura o error
        CancelacionPedido = 5 // El pedido se canceló y el stock vuelve
    }

    public class MovimientoStock
    {
        public int MovimientoStockId { get; set; }

        [Required]
        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        [Required]
        public int Cantidad { get; set; }

        [Required]
        public TipoMovimiento Tipo { get; set; }

        public DateTime Fecha { get; set; } = DateTime.Now;

        [MaxLength(250)]
        public string Observaciones { get; set; }

        // Relacionar con un Pedido específico si es una venta
        public int? PedidoId { get; set; }
        public virtual Pedido? Pedido { get; set; }
    }
}

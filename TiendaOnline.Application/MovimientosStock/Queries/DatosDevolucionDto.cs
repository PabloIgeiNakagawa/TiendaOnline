using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.MovimientosStock.Queries
{
    /// <summary>
    /// Datos del pedido para el formulario de devolución del panel: los productos
    /// del pedido con lo comprado y lo ya devuelto por producto. La suma de
    /// devueltas viene de los movimientos de tipo Devolucion (el único camino
    /// que suma stock con traza); el guard del command service es quien decide
    /// al escribir: este DTO es espejo informativo sobre los mismos datos.
    /// </summary>
    public class DatosDevolucionDto
    {
        public int PedidoId { get; set; }
        public EstadoPedido Estado { get; set; }
        public string? ClienteNombre { get; set; }

        // Calculado server-side: Enviado/Entregado. La vista NO calcula permisos.
        public bool AdmiteDevolucion { get; set; }

        public List<ItemDevolucionDto> Items { get; set; } = new();
    }

    public class ItemDevolucionDto
    {
        public int ProductoId { get; set; }
        public string ProductoNombre { get; set; } = string.Empty;
        public int CantidadComprada { get; set; }
        public int CantidadDevuelta { get; set; }

        // El tope que admite otra devolución de este producto en este pedido.
        public int QuedanPorDevolver => CantidadComprada - CantidadDevuelta;
    }
}

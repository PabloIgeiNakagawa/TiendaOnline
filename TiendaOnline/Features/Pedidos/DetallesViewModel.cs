using TiendaOnline.Domain.Entities;
using TiendaOnline.Application.Usuarios.Queries;

namespace TiendaOnline.Features.Pedidos
{
    public class PedidoDetalleViewModel
    {
        public int PedidoId { get; set; }
        public string NumeroPedido => PedidoId.ToString("D6");

        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnPreparacion { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelado { get; set; }

        public EstadoPedido Estado { get; set; }
        public EstadoPago EstadoPago { get; set; }
        public MetodoPago MetodoPagoId { get; set; }

        // Usuario
        public string? UsuarioNombre { get; set; }
        public string? UsuarioEmail { get; set; }
        public string? UsuarioTelefono { get; set; }

        // Dirección de envío (si aplica)
        public string? DireccionCompleta { get; set; }
        public string? Observaciones { get; set; }

        // Reparto: asignación (select del admin) y seguimiento en vivo del mapa.
        public int? RepartidorId { get; set; }
        public string? RepartidorNombre { get; set; }
        public List<RepartidorResumenDto> RepartidoresDisponibles { get; set; } = new();

        public bool PuedeAsignarRepartidor =>
            EsAdmin && (Estado == EstadoPedido.EnPreparacion || Estado == EstadoPedido.Enviado);

        /// <summary>El mapa solo se muestra mientras hay una entrega en camino.</summary>
        public bool MostrarMapa => Estado == EstadoPedido.Enviado;

        // Productos
        public List<PedidoItemViewModel> Items { get; set; } = new();

        // Totales ya calculados
        public decimal Subtotal { get; set; }
        public decimal CostoEnvio { get; set; }
        public decimal Total { get; set; }

        // Tracking
        public string? NumeroSeguimiento { get; set; }
        public DateTime? FechaEstimadaEntrega { get; set; }

        // Comprobante de pago
        public string? TransaccionPagoId { get; set; }
        public string? ComprobanteUrl { get; set; }
        public DateTime? FechaComprobante { get; set; }

        // Permisos (la vista NO debería calcular esto)
        public bool EsAdmin { get; set; }
        public bool EsRepartidor { get; set; }
        public bool EsPropioPedido { get; set; }

        /// <summary>El repartidor logueado es quien lleva este pedido.</summary>
        public bool EsMiEntrega { get; set; }

        // Estados útiles para la vista
        public bool PuedeCancelar { get; set; }
        public bool PuedeEnviar { get; set; }
        public bool PuedeEntregar { get; set; }
    }

    public class PedidoItemViewModel
    {
        public string ProductoNombre { get; set; } = string.Empty;
        public string ProductoImagenUrl { get; set; } = string.Empty;

        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}

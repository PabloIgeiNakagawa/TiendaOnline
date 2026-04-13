using TiendaOnline.Enums;

namespace TiendaOnline.Features.Pedidos
{
    public class PedidoDetalleViewModel
    {
        public int PedidoId { get; set; }
        public string NumeroPedido => PedidoId.ToString("D6");

        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelado { get; set; }

        public EstadoPedidoUI Estado { get; set; }
        public EstadoPagoUI EstadoPago { get; set; }
        public MetodoPagoId MetodoPagoId { get; set; }

        // Usuario
        public string? UsuarioNombre { get; set; }
        public string? UsuarioEmail { get; set; }
        public string? UsuarioTelefono { get; set; }

        // Dirección de envío (si aplica)
        public string? DireccionCompleta { get; set; }
        public string? Observaciones { get; set; }

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

        // Permisos (la vista NO debería calcular esto)
        public bool EsAdmin { get; set; }
        public bool EsRepartidor { get; set; }
        public bool EsPropioPedido { get; set; }

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

using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Application.Pedidos.Query
{
    public class PedidoDetallesDto
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelado { get; set; }

        public int EstadoId { get; set; }
        public string EstadoNombre { get; set; } = string.Empty;

        // Usuario
        public int? UsuarioId { get; set; }
        public string? UsuarioNombre { get; set; }
        public string? UsuarioEmail { get; set; }
        public string? UsuarioTelefono { get; set; }

        // Dirección de envío (si aplica)
        public string? DireccionCompleta { get; set; }
        public string? Localidad { get; set; }
        public string? Provincia { get; set; }

        // Items
        public List<PedidoItemDto> Items { get; set; } = new();
    }

    public class PedidoItemDto
    {
        public string ProductoNombre { get; set; } = string.Empty;
        public string ProductoImagenUrl { get; set; } = string.Empty;

        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}

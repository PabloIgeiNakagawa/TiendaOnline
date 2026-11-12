namespace TiendaOnline.Application.Pedidos.Queries
{
    /// <summary>
    /// Fila de la cola de reparto: lo que necesita la entrega (cliente, contacto,
    /// dirección y observación) sin recorrer el detalle completo del pedido.
    /// </summary>
    public class PedidoRepartoDto
    {
        public int PedidoId { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public bool EsEnvioADomicilio { get; set; }
        public string? DireccionCompleta { get; set; }
        public string? Observaciones { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public int CantidadItems { get; set; }
        public List<PedidoProductoListadoDto> Productos { get; set; } = new();
    }
}

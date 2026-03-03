namespace TiendaOnline.Features.Tienda.Pedidos
{
    public class MisPedidosViewModel
    {
        public List<PedidoListaViewModel> Pedidos { get; set; } = new();
    }

    public class PedidoListaViewModel
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelado { get; set; }
        public List<string> Productos { get; set; } = new();

        public string Estado { get; set; } = string.Empty;

        public string EstadoCss { get; set; } = string.Empty;

        public bool EstaEnviado => FechaEnvio.HasValue;
        public bool EstaEntregado => FechaEntrega.HasValue;
        public bool EstaCancelado => FechaCancelado.HasValue;
    }
}
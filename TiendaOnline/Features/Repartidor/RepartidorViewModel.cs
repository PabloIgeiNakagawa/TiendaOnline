using TiendaOnline.Application.Pedidos.Queries;

namespace TiendaOnline.Features.Repartidor
{
    /// <summary>
    /// La cola del reparto de un vistazo: los entregables arriba y los entregados
    /// de las últimas 24 horas abajo, en la sección plegable del historial.
    /// </summary>
    public class RepartidorViewModel
    {
        public List<PedidoRepartoDto> Pendientes { get; set; } = new();
        public List<PedidoRepartoDto> EntregadosUltimoDia { get; set; } = new();
    }
}

using TiendaOnline.Application.Pedidos.Queries;

namespace TiendaOnline.Application.Common.Interfaces
{
    public interface IComprobantePdfService
    {
        Task<byte[]> GenerarComprobanteAsync(ComprobantePedidoDto datos);
    }
}

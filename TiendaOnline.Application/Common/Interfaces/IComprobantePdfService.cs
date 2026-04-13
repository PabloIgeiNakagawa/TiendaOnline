using TiendaOnline.Application.Pedidos.DTOs;

namespace TiendaOnline.Application.Common.Interfaces
{
    public interface IComprobantePdfService
    {
        Task<byte[]> GenerarComprobanteAsync(ComprobantePedidoDto datos);
    }
}

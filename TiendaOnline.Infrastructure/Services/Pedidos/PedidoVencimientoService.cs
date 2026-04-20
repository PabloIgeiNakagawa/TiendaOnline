using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class PedidoVencimientoService : IPedidoVencimientoService
    {
        private readonly TiendaContext _context;
        private readonly ILogger<PedidoVencimientoService> _logger;

        public PedidoVencimientoService(TiendaContext context, ILogger<PedidoVencimientoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<int> CancelarPedidosVencidosAsync()
        {
            var fechaLimite = DateTime.Now.Subtract(Pedido.TiempoMaximoPagoPendiente);
            var pedidosVencidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                .Where(p => p.Estado == EstadoPedido.Nuevo
                    && p.EstadoPago != EstadoPago.Aprobado
                    && p.FechaPedido <= fechaLimite)
                .ToListAsync();

            if (pedidosVencidos.Count == 0)
                return 0;

            foreach (var pedido in pedidosVencidos)
            {
                pedido.Estado = EstadoPedido.Cancelado;
                pedido.FechaCancelado = pedido.FechaCancelado ?? DateTime.Now;
            }

            foreach (var pedido in pedidosVencidos)
            {
                var subtotal = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);
                var total = subtotal + pedido.CostoEnvio;
                var nombreCompletoUsuario = string.Join(" ", new[] { pedido.Usuario.Nombre, pedido.Usuario.Apellido }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPedidoVencidoAsync(
                    pedido.Usuario.Email,
                    nombreCompletoUsuario,
                    pedido.PedidoId,
                    total));
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Se cancelaron {Cantidad} pedidos vencidos por falta de pago.", pedidosVencidos.Count);
            return pedidosVencidos.Count;
        }
    }
}

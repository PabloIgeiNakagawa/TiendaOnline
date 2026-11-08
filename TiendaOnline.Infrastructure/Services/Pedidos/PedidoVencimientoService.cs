using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Pedidos.Commands;
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
            // Snapshot proyectado en un solo roundtrip y sin tracking: solo los
            // datos que necesita el aviso (id del pedido + datos del email).
            // Cada candidato se adjudica después con un UPDATE condicional que
            // re-verifica las precondiciones al momento de escribir: leer y
            // guardar directo contra el snapshot permitiría que un pago que
            // aprueba en paralelo termine cancelado encima.
            var fechaLimite = DateTime.Now.Subtract(Pedido.TiempoMaximoPagoPendiente);
            var candidatos = await _context.Pedidos
                .Where(p => p.Estado == EstadoPedido.Nuevo
                    && p.EstadoPago != EstadoPago.Aprobado
                    && p.FechaPedido <= fechaLimite)
                .Select(p => new
                {
                    p.PedidoId,
                    Email = p.Usuario.Email,
                    Nombre = p.Usuario.Nombre,
                    Apellido = p.Usuario.Apellido,
                    Total = p.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario) + p.CostoEnvio
                })
                .ToListAsync();

            if (candidatos.Count == 0)
                return 0;

            var emailsVencidos = new List<(string Email, int PedidoId, decimal Total)>();

            foreach (var candidato in candidatos)
            {
                // El UPDATE condicional repite todas las precondiciones del
                // snapshot: si entre el snapshot y acá el pedido ganó un pago o
                // cambió de estado, 0 filas y el pedido queda intacto.
                var filas = await _context.Pedidos
                    .Where(p => p.PedidoId == candidato.PedidoId
                        && p.Estado == EstadoPedido.Nuevo
                        && p.EstadoPago != EstadoPago.Aprobado
                        && p.FechaPedido <= fechaLimite)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Estado, EstadoPedido.Cancelado)
                        .SetProperty(p => p.FechaCancelado, p => p.FechaCancelado ?? DateTime.Now));

                if (filas == 0)
                {
                    _logger.LogInformation("El pedido {PedidoId} ya no está cancelable (pudo haberse pagado durante la corrida del job): no se cancela.", candidato.PedidoId);
                    continue;
                }

                emailsVencidos.Add((candidato.Email, candidato.PedidoId, candidato.Total));
            }

            // Emails recién DESPUÉS de que la escritura ganó, y por pedido ganado:
            // guardaba que un save fallido dejara avisos de "vencido" encolados para
            // pedidos que quedaron enteros sin cancelar en la base.
            foreach (var e in emailsVencidos)
            {
                var candidato = candidatos.Single(c => c.PedidoId == e.PedidoId);
                var nombreCompletoUsuario = string.Join(" ", new[] { candidato.Nombre, candidato.Apellido }
                    .Where(x => !string.IsNullOrWhiteSpace(x)));

                BackgroundJob.Enqueue<IEmailService>(x => x.EnviarEmailPedidoVencidoAsync(
                    e.Email,
                    nombreCompletoUsuario,
                    e.PedidoId,
                    e.Total));
            }

            _logger.LogInformation("Se cancelaron {Cantidad} pedidos vencidos por falta de pago.", emailsVencidos.Count);
            return emailsVencidos.Count;
        }
    }
}

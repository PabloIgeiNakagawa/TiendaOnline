using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    /// <summary>
    /// Asignación del reparto: el repartidor toma un pedido Enviado que nadie
    /// tomó (o suelta el suyo) y el admin asigna/reasigna desde el panel. Todos
    /// los cambios son claims condicionales: dos tomas simultáneas no pueden
    /// dejar el mismo pedido en dos manos.
    /// </summary>
    public class AsignacionRepartidorService : IAsignacionRepartidorService
    {
        private readonly TiendaContext _context;
        private readonly IUserSessionService _userSessionService;
        private readonly ILogger<AsignacionRepartidorService> _logger;

        public AsignacionRepartidorService(TiendaContext context, IUserSessionService userSessionService, ILogger<AsignacionRepartidorService> logger)
        {
            _context = context;
            _userSessionService = userSessionService;
            _logger = logger;
        }

        public async Task TomarPedidoAsync(int pedidoId, CancellationToken ct = default)
        {
            var repartidorId = _userSessionService.GetUserId()
                ?? throw new UsuarioNoEncontradoException("No hay usuario en la sesión.");

            var filas = await _context.Pedidos
                .Where(p => p.PedidoId == pedidoId
                    && p.Estado == EstadoPedido.Enviado
                    && p.RepartidorId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.RepartidorId, repartidorId), ct);

            if (filas > 0)
            {
                _logger.LogInformation("Repartidor {RepartidorId} tomó el pedido {PedidoId}", repartidorId, pedidoId);
                return;
            }

            var real = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new { p.Estado, p.RepartidorId })
                .FirstOrDefaultAsync(ct);

            if (real == null) throw new PedidoNoEncontradoException("Pedido no encontrado.");

            // Doble click del mismo repartidor: ya era suyo, no es un error.
            if (real.Estado == EstadoPedido.Enviado && real.RepartidorId == repartidorId) return;

            if (real.Estado != EstadoPedido.Enviado)
                throw new PedidoNoAsignableException($"El pedido no está en camino (Estado actual: {real.Estado}).");

            throw new PedidoNoAsignableException("Otro repartidor ya tomó este pedido.");
        }

        public async Task LiberarPedidoAsync(int pedidoId, CancellationToken ct = default)
        {
            var repartidorId = _userSessionService.GetUserId()
                ?? throw new UsuarioNoEncontradoException("No hay usuario en la sesión.");

            var filas = await _context.Pedidos
                .Where(p => p.PedidoId == pedidoId
                    && p.Estado == EstadoPedido.Enviado
                    && p.RepartidorId == repartidorId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.RepartidorId, (int?)null), ct);

            if (filas == 0)
            {
                var real = await _context.Pedidos
                    .AsNoTracking()
                    .Where(p => p.PedidoId == pedidoId)
                    .Select(p => new { p.Estado, p.RepartidorId })
                    .FirstOrDefaultAsync(ct);

                if (real == null) throw new PedidoNoEncontradoException("Pedido no encontrado.");

                // Ya estaba sin repartidor: el doble click es benigno.
                if (real.Estado != EstadoPedido.Enviado || real.RepartidorId == null) return;

                throw new PedidoNoAsignableException("El pedido no está asignado a vos.");
            }

            await UbicacionDeRepartidor.LimpiarSiSinEntregasAsync(_context, repartidorId, ct);
        }

        public async Task AsignarRepartidorAsync(int pedidoId, int? repartidorId, CancellationToken ct = default)
        {
            if (repartidorId.HasValue)
            {
                var esRepartidorActivo = await _context.Usuarios
                    .AnyAsync(u => u.UsuarioId == repartidorId.Value && u.Activo && u.Rol == Rol.Repartidor, ct);

                if (!esRepartidorActivo)
                    throw new PedidoNoAsignableException("El usuario elegido no es un repartidor activo.");
            }

            var anterior = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new { p.Estado, p.RepartidorId })
                .FirstOrDefaultAsync(ct);

            if (anterior == null) throw new PedidoNoEncontradoException("Pedido no encontrado.");

            // Preparación y Enviado son los estados donde alguien puede llevar
            // el pedido: antes no hay caja armada y después ya se entregó.
            var filas = await _context.Pedidos
                .Where(p => p.PedidoId == pedidoId
                    && (p.Estado == EstadoPedido.EnPreparacion || p.Estado == EstadoPedido.Enviado))
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.RepartidorId, repartidorId), ct);

            if (filas == 0)
                throw new PedidoNoAsignableException($"El pedido no se puede asignar en estado {anterior.Estado}.");

            if (anterior.RepartidorId.HasValue && anterior.RepartidorId != repartidorId)
                await UbicacionDeRepartidor.LimpiarSiSinEntregasAsync(_context, anterior.RepartidorId.Value, ct);

            _logger.LogInformation("Pedido {PedidoId} asignado a {RepartidorId}", pedidoId, repartidorId);
        }
    }
}

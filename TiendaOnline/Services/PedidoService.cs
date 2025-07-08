using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Diagnostics;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;

namespace TiendaOnline.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly TiendaContext _context;
        private readonly IAuditoriaService _auditoriaService;
        private readonly int usuarioActivoId;

        public PedidoService(TiendaContext context, IAuditoriaService auditoriaService)
        {
            _context = context;
            _auditoriaService = auditoriaService;
            usuarioActivoId = _auditoriaService.ObtenerUsuarioId();
        }

        public async Task<Pedido?> ObtenerPedidoAsync(int id)
        {
            return await _context.Pedidos.FindAsync(id);
        }

        public async Task<List<Pedido>> ObtenerPedidosDeUsuarioAsync(int id)
        {
            return await _context.Pedidos.Where(p => p.UsuarioId == id).ToListAsync();
        }

        public async Task<List<Pedido>> ObtenerPedidosAsync()
        {
            return await _context.Pedidos.ToListAsync();
        }

        public async Task<List<Pedido>> ObtenerPedidosConDetallesAsync()
        {
            return await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                .ThenInclude(d => d.Producto)
                .ToListAsync();
        }

        public async Task<Pedido?> ObtenerPedidoConDetallesAsync(int id)
        {
            return await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == id);
        }

        public async Task<int> CrearPedidoAsync(List<ItemCarrito> carrito, int usuarioId)
        {
            var pedido = new Pedido
            {
                UsuarioId = usuarioActivoId,
                FechaPedido = DateTime.Now,
                Estado = EstadoPedido.Pendiente,
                DetallesPedido = carrito.Select(item => new DetallePedido
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.Precio
                }).ToList()
            };

            _context.Pedidos.Add(pedido);
            int cambios = await _context.SaveChangesAsync();
            if (cambios > 0)
            {
                var auditoria = new Auditoria
                {
                    UsuarioId = usuarioActivoId,
                    Accion = "Crear Pedido",
                    DatosAnteriores = "{}",
                    DatosNuevos = JsonConvert.SerializeObject(pedido),
                    Fecha = DateTime.Now
                };
                await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
                return pedido.PedidoId;
            }
            else
            {
                throw new Exception("No se pudo crear el pedido.");

            }
        }

        public async Task PedidoEnviadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null)
                throw new Exception("Pedido no encontrado.");

            EstadoPedido estadoAnterior = pedido.Estado;

            pedido.Estado = EstadoPedido.Enviado;
            pedido.FechaEnvio = DateTime.Now;
            int cambios = await _context.SaveChangesAsync();
            if(cambios > 0)
            {
                var auditoria = new Auditoria
                {
                    UsuarioId = usuarioActivoId,
                    Accion = "Enviar Pedido",
                    DatosAnteriores = JsonConvert.SerializeObject(new { Estado = estadoAnterior, FechaEntrega = (DateTime?)null }),
                    DatosNuevos = JsonConvert.SerializeObject(new { Estado = pedido.Estado, FechaEntrega = pedido.FechaEntrega }),
                    Fecha = DateTime.Now
                };
                await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
            }
        }

        public async Task PedidoEntregadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null)
                throw new Exception("Pedido no encontrado.");

            EstadoPedido estadoAnterior = pedido.Estado;

            pedido.Estado = EstadoPedido.Entregado;
            pedido.FechaEntrega = DateTime.Now;
            int cambios = await _context.SaveChangesAsync();

            if (cambios > 0)
            {
                var auditoria = new Auditoria
                {
                    UsuarioId = usuarioActivoId,
                    Accion = "Entregar Pedido",
                    DatosAnteriores = JsonConvert.SerializeObject(new { Estado = estadoAnterior, FechaEntrega = (DateTime?)null }),
                    DatosNuevos = JsonConvert.SerializeObject(new { Estado = pedido.Estado, FechaEntrega = pedido.FechaEntrega }),
                    Fecha = DateTime.Now
                };
                await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
            }
            else
            {
                throw new Exception("No se pudo entregar el pedido.");
            }
        }

        public async Task PedidoCanceladoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null)
                throw new Exception("Pedido no encontrado.");

            EstadoPedido estadoAnterior = pedido.Estado;

            pedido.Estado = EstadoPedido.Cancelado;
            pedido.FechaCancelado = DateTime.Now;

            int cambios = await _context.SaveChangesAsync();
            if (cambios > 0)
            {
                var auditoria = new Auditoria
                {
                    UsuarioId = usuarioActivoId,
                    Accion = "Cancelar Pedido",
                    DatosAnteriores = JsonConvert.SerializeObject(new { Estado = estadoAnterior, FechaCancelado = (DateTime?)null }),
                    DatosNuevos = JsonConvert.SerializeObject(new { Estado = pedido.Estado, FechaCancelado = pedido.FechaCancelado }),
                    Fecha = DateTime.Now
                };
                await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
            }
            else
            {
                throw new Exception("No se pudo cancelar el pedido.");
            }
        }
    }
}

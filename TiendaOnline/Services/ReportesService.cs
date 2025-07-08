using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;

namespace TiendaOnline.Services
{
    public class ReportesService : IReportesService
    {
        private readonly TiendaContext _context;
        public ReportesService(TiendaContext context)
        {
            _context = context;
        }
        public async Task<DashboardViewModel> ObtenerDashboardAsync(int periodo)
        {
            var desdeFecha = periodo switch
            {
                1 => DateTime.Now.AddMonths(-1),
                3 => DateTime.Now.AddMonths(-3),
                6 => DateTime.Now.AddMonths(-6),
                12 => DateTime.Now.AddYears(-1),
                _ => DateTime.MinValue
            };

            var pedidos = await _context.Pedidos
                .Include(p => p.DetallesPedido)
                    .ThenInclude(dp => dp.Producto)
                .Include(p => p.Usuario)
                .Where(p => p.FechaPedido >= desdeFecha)
                .ToListAsync();

            // Top productos
            var productos = pedidos
                .SelectMany(p => p.DetallesPedido)
                .GroupBy(dp => dp.Producto.Nombre)
                .Select(g => new { Producto = g.Key, Total = g.Sum(x => x.Cantidad) })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            // Por estado
            var porEstado = pedidos
                .GroupBy(p => p.Estado.ToString())
                .Select(g => new { Estado = g.Key, Cantidad = g.Count() })
                .ToList();

            // Por categoría
            var porCategoria = pedidos
                .SelectMany(p => p.DetallesPedido)
                .GroupBy(dp => dp.Producto.CategoriaId)
                .Select(g => new
                {
                    Categoria = _context.Categorias.FirstOrDefault(c => c.CategoriaId == g.Key)?.Nombre ?? "Sin categoría",
                    Total = g.Sum(x => x.PrecioUnitario * x.Cantidad)
                })
                .ToList();

            // Top clientes
            var clientes = pedidos
                .GroupBy(p => p.Usuario.Email)
                .Select(g => new { Cliente = g.Key, Total = g.Count() })
                .OrderByDescending(x => x.Total)
                .Take(5)
                .ToList();

            // Cancelados
            var cancelados = pedidos.Count(p => p.Estado == EstadoPedido.Cancelado);
            var porcentajeCancelados = pedidos.Any() ? (decimal)cancelados * 100 / pedidos.Count : 0;

            return new DashboardViewModel
            {
                TopProductos = productos.Select(x => x.Producto).ToList(),
                VentasPorProducto = productos.Select(x => x.Total).ToList(),
                EstadosPedido = porEstado.Select(x => x.Estado).ToList(),
                CantidadPorEstado = porEstado.Select(x => x.Cantidad).ToList(),
                Categorias = porCategoria.Select(x => x.Categoria).ToList(),
                VentasPorCategoria = porCategoria.Select(x => x.Total).ToList(),
                TopClientes = clientes.Select(x => x.Cliente).ToList(),
                PedidosPorCliente = clientes.Select(x => x.Total).ToList(),
                CantidadCancelados = cancelados,
                PorcentajeCancelados = Math.Round(porcentajeCancelados, 2)
            };
        }
    }

}

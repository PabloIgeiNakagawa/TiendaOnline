using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Models;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Services
{
    public class ReportesServiceTests : IDisposable
    {
        private readonly TiendaContext _db;
        private readonly ReportesService _service;

        public ReportesServiceTests()
        {
            var options = new DbContextOptionsBuilder<TiendaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new TiendaContext(options);
            _service = new ReportesService(_db);
        }

        [Fact]
        public async Task ObtenerDashboardAsync_DebeCalcularTopProductosCorrectamente()
        {
            // ARRANGE - Creamos un escenario con ventas
            var productoA = new Producto { ProductoId = 1, Nombre = "Producto A" , Descripcion = "Descripcion Producto A" };
            var productoB = new Producto { ProductoId = 2, Nombre = "Producto B", Descripcion = "Descripcion Producto B" };

            var usuario = new Usuario { UsuarioId = 1, Email = "test@test.com", Nombre = "A", Apellido = "B", Telefono = "1", Contrasena = "1" };

            var pedido = new Pedido
            {
                PedidoId = 1,
                UsuarioId = 1,
                FechaPedido = DateTime.Now,
                DetallesPedido = new List<DetallePedido>
                {
                    new DetallePedido { Producto = productoA, Cantidad = 10, PrecioUnitario = 100 },
                    new DetallePedido { Producto = productoB, Cantidad = 5, PrecioUnitario = 100 }
                }
            };

            _db.Usuarios.Add(usuario);
            _db.Pedidos.Add(pedido);
            await _db.SaveChangesAsync();

            // ACT - Pedimos reporte del último mes (periodo 1)
            var dashboard = await _service.ObtenerDashboardAsync(1);

            // ASSERT
            Assert.Equal("Producto A", dashboard.TopProductos[0]);
            Assert.Equal(10, dashboard.VentasPorProducto[0]); // El más vendido
            Assert.Equal(5, dashboard.VentasPorProducto[1]);
        }

        [Fact]
        public async Task ObtenerDashboardAsync_DebeFiltrarPorPeriodo()
        {
            // ARRANGE - Un pedido hoy y uno hace 2 años
            var usuario = new Usuario { UsuarioId = 1, Email = "user@test.com", Nombre = "A", Apellido = "B", Telefono = "1", Contrasena = "1" };

            var pedidoReciente = new Pedido { PedidoId = 1, UsuarioId = 1, FechaPedido = DateTime.Now, Estado = EstadoPedido.Entregado };
            var pedidoAntiguo = new Pedido { PedidoId = 2, UsuarioId = 1, FechaPedido = DateTime.Now.AddYears(-2), Estado = EstadoPedido.Entregado };

            _db.Usuarios.Add(usuario);
            _db.Pedidos.AddRange(pedidoReciente, pedidoAntiguo);
            await _db.SaveChangesAsync();

            // ACT - Filtramos por el último mes (periodo 1)
            var dashboard = await _service.ObtenerDashboardAsync(1);

            // ASSERT
            // Solo debería contar el pedido reciente en las estadísticas generales de conteo
            Assert.Equal(1, dashboard.CantidadPorEstado.Sum());
        }

        [Fact]
        public async Task ObtenerDashboardAsync_DebeCalcularPorcentajeCancelados()
        {
            // ARRANGE - 4 pedidos, 1 cancelado (25%)
            var usuario = new Usuario { UsuarioId = 1, Email = "test@test.com", Nombre = "A", Apellido = "B", Telefono = "1", Contrasena = "1" };
            _db.Usuarios.Add(usuario);

            for (int i = 1; i <= 3; i++)
                _db.Pedidos.Add(new Pedido { PedidoId = i, UsuarioId = 1, Estado = EstadoPedido.Entregado, FechaPedido = DateTime.Now });

            _db.Pedidos.Add(new Pedido { PedidoId = 4, UsuarioId = 1, Estado = EstadoPedido.Cancelado, FechaPedido = DateTime.Now });

            await _db.SaveChangesAsync();

            // ACT
            var dashboard = await _service.ObtenerDashboardAsync(0); // 0 = Todo el histórico

            // ASSERT
            Assert.Equal(25, dashboard.PorcentajeCancelados);
            Assert.Equal(1, dashboard.CantidadCancelados);
        }

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }
    }
}
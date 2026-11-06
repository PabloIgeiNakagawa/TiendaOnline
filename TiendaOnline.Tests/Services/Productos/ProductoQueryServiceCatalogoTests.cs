using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Services.Productos;
using TiendaOnline.Infrastructure.Services.Ofertas;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Productos
{
    /// <summary>
    /// Tests del catálogo: el orden "precio asc/desc" y los filtros PrecioMin/PrecioMax
    /// corren sobre el precio efectivo (mejor oferta vigente ?? lista), no sobre el de
    /// lista. La subquery de EF se traduce: contra la DB real (SQLite in-memory).
    /// </summary>
    public class ProductoQueryServiceCatalogoTests
    {
        private static ProductoQueryService Sut(TiendaOnline.Infrastructure.Persistence.TiendaContext ctx)
            => new(ctx, new OfertaQueryService(ctx));

        private static async Task<(Producto Lista70, Producto ConOferta90Lista120)> ArmarCatalogo(
            TiendaOnline.Infrastructure.Persistence.TiendaContext ctx)
        {
            var sinOferta = TestDatabase.SeedProducto(ctx, precio: 70m, stock: 5, nombre: "Sin oferta");
            var conOferta = TestDatabase.SeedProducto(ctx, precio: 120m, stock: 5, nombre: "Con oferta");
            TestDatabase.SeedOferta(ctx, conOferta, precioOferta: 90m);
            return (sinOferta, conOferta);
        }

        [Fact]
        public async Task Catalogo_OrdenPrecioAsc_ProductoConOfertaQuedaSegunSuPrecioEfectivo()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (sinOferta, conOferta) = await ArmarCatalogo(ctx);
            var sut = Sut(ctx);

            var resultado = await sut.ObtenerProductosCatalogoAsync(new ObtenerProductosCatalogoRequest
            {
                Pagina = 1,
                Cantidad = 10,
                Orden = "precio-asc"
            }, CancellationToken.None);
            var ids = resultado.Items.Select(i => i.ProductoId).ToList();

            // Precio efectivo: 70 < 90 → sin oferta primero (conOferta pesa por su oferta,
            // no por su lista de 120).
            Assert.NotEqual(ids.IndexOf(sinOferta.ProductoId), ids.IndexOf(conOferta.ProductoId));
            Assert.Equal(sinOferta.ProductoId, ids.First());
            Assert.Equal(conOferta.ProductoId, ids.Last());
        }

        [Fact]
        public async Task Catalogo_OrdenPrecioDesc_EfectivoGanaSobrePrecioDeLista()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (sinOferta, conOferta) = await ArmarCatalogo(ctx);
            var sut = Sut(ctx);

            // Dos productos más para que el orden discrimine métricas: por lista la
            // promocionada (150) iría última; por efectivo (40 con oferta) va primera.
            var lista60 = TestDatabase.SeedProducto(ctx, precio: 60m, stock: 5, nombre: "Lista 60");
            var promocionada = TestDatabase.SeedProducto(ctx, precio: 150m, stock: 5, nombre: "Promo");
            TestDatabase.SeedOferta(ctx, promocionada, precioOferta: 40m);

            var resultado = await sut.ObtenerProductosCatalogoAsync(new ObtenerProductosCatalogoRequest
            {
                Pagina = 1,
                Cantidad = 10,
                Orden = "precio-asc"
            }, CancellationToken.None);
            var ids = resultado.Items.Select(i => i.ProductoId).ToList();

            // efectivo: 40 (promo) < 60 < 70 < 90 → orden de efectivo, no de lista.
            Assert.Equal(new[] { promocionada.ProductoId, lista60.ProductoId, sinOferta.ProductoId, conOferta.ProductoId }, ids);
        }

        [Fact]
        public async Task Catalogo_FiltroPrecioMin_CuentaEntrarLaOferta()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (sinOferta, conOferta) = await ArmarCatalogo(ctx);
            var sut = Sut(ctx);

            var resultado = await sut.ObtenerProductosCatalogoAsync(new ObtenerProductosCatalogoRequest
            {
                Pagina = 1,
                Cantidad = 10,
                PrecioMin = 100m
            }, CancellationToken.None);

            // Efectivo: conOferta = 90 < 100 queda fuera (por lista 120 habría entrado);
            // sinOferta = 70 < 100 también → no queda nada.
            Assert.Empty(resultado.Items);
        }

        [Fact]
        public async Task Catalogo_FiltroPrecioMax_CuentaEntrarLaOferta()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var (sinOferta, conOferta) = await ArmarCatalogo(ctx);
            var sut = Sut(ctx);

            var resultado = await sut.ObtenerProductosCatalogoAsync(new ObtenerProductosCatalogoRequest
            {
                Pagina = 1,
                Cantidad = 10,
                PrecioMax = 100m
            }, CancellationToken.None);

            // Con lista: solo sinOferta (70) entra. Con efectivo: también entra conOferta (90 ≤ 100).
            var ids = resultado.Items.Select(i => i.ProductoId).ToList();
            Assert.Contains(sinOferta.ProductoId, ids);
            Assert.Contains(conOferta.ProductoId, ids);
        }

        [Fact]
        public async Task Catalogo_FiltroSobreOfertaExpirada_CorrenSobrePrecioDeLista()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 200m, stock: 5);
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 100m,
                inicio: DateTime.Now.AddDays(-10), fin: DateTime.Now.AddDays(-5)); // expirada
            var sut = Sut(ctx);

            var resultado = await sut.ObtenerProductosCatalogoAsync(new ObtenerProductosCatalogoRequest
            {
                Pagina = 1,
                Cantidad = 10,
                PrecioMax = 100m
            }, CancellationToken.None);

            // La oferta expirada no cuenta para el filtro: efectivo = lista 200 > 100, fuera.
            Assert.Empty(resultado.Items);
        }
    }
}

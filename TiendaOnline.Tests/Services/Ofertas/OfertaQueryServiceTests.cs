using Microsoft.EntityFrameworkCore;
using TiendaOnline.Infrastructure.Services.Ofertas;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Ofertas
{
    /// <summary>
    /// Tests del resolver de precio vigente: la decisión de cuánto se cobra
    /// sale de esta query contra la DB real (SQLite in-memory), por eso
    /// no se mockea: Activa + ventana de vigencia + gana la más barata.
    /// </summary>
    public class OfertaQueryServiceTests
    {
        private static OfertaQueryService Sut(TiendaOnline.Infrastructure.Persistence.TiendaContext ctx)
            => new(ctx);

        // ------------------------------------------------------------------
        // Oferta vigente
        // ------------------------------------------------------------------

        [Fact]
        public async Task ObtenerOfertaVigente_OfertaActivaEnVentana_RetornaPrecioOferta()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 79.9m);
            var sut = Sut(ctx);

            var oferta = await sut.ObtenerOfertaVigenteAsync(producto.ProductoId);

            Assert.NotNull(oferta);
            Assert.Equal(79.9m, oferta!.PrecioOferta);
            Assert.Equal(producto.ProductoId, oferta.ProductoId);
        }

        [Fact]
        public async Task ObtenerOfertaVigente_SinOfertas_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var sut = Sut(ctx);

            var oferta = await sut.ObtenerOfertaVigenteAsync(producto.ProductoId);

            Assert.Null(oferta); // el caller usa precio de lista
        }

        [Fact]
        public async Task ObtenerOfertaVigente_OfertaExpirada_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 50m,
                inicio: DateTime.Now.AddDays(-10), fin: DateTime.Now.AddDays(-1));
            var sut = Sut(ctx);

            var oferta = await sut.ObtenerOfertaVigenteAsync(producto.ProductoId);

            Assert.Null(oferta);
        }

        [Fact]
        public async Task ObtenerOfertaVigente_OfertaFutura_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 50m,
                inicio: DateTime.Now.AddDays(1), fin: DateTime.Now.AddDays(7));
            var sut = Sut(ctx);

            var oferta = await sut.ObtenerOfertaVigenteAsync(producto.ProductoId);

            Assert.Null(oferta);
        }

        [Fact]
        public async Task ObtenerOfertaVigente_OfertaDesactivada_RetornaNull()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 50m, activa: false);
            var sut = Sut(ctx);

            var oferta = await sut.ObtenerOfertaVigenteAsync(producto.ProductoId);

            Assert.Null(oferta);
        }

        [Fact]
        public async Task ObtenerOfertaVigente_VariasSolapadas_GanaLaMasBarata()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var ahora = DateTime.Now;
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 85m, inicio: ahora.AddHours(-2), fin: ahora.AddHours(2));
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 69.9m, inicio: ahora.AddHours(-1), fin: ahora.AddHours(1));
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 90m, inicio: ahora.AddHours(-3), fin: ahora.AddHours(3));
            var sut = Sut(ctx);

            var oferta = await sut.ObtenerOfertaVigenteAsync(producto.ProductoId);

            Assert.NotNull(oferta);
            Assert.Equal(69.9m, oferta!.PrecioOferta); // la más barata de las vigentes
        }

        [Fact]
        public async Task ObtenerOfertaVigente_SolapadasPeroUnaExpiraAhora_SoloCuentaLaVigente()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var ahora = DateTime.Now;
            // Barata pero ya expirada; la vigente es más cara que la expirada.
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 40m, inicio: ahora.AddDays(-5), fin: ahora.AddDays(-1));
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 80m, inicio: ahora.AddHours(-1), fin: ahora.AddHours(5));
            var sut = Sut(ctx);

            var oferta = await sut.ObtenerOfertaVigenteAsync(producto.ProductoId);

            Assert.NotNull(oferta);
            Assert.Equal(80m, oferta!.PrecioOferta); // la expirada NO compite
        }

        // ------------------------------------------------------------------
        // Listado admin (estado calculado)
        // ------------------------------------------------------------------

        [Fact]
        public async Task ObtenerTodas_ConVariasOfertas_EstadoCalculadoCorrecto()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var ahora = DateTime.Now;
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 80m, inicio: ahora.AddHours(-1), fin: ahora.AddDays(1));                      // Vigente
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 70m, inicio: ahora.AddDays(2), fin: ahora.AddDays(3));                        // Futura
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 60m, inicio: ahora.AddDays(-3), fin: ahora.AddDays(-2));                      // Expirada
            TestDatabase.SeedOferta(ctx, producto, precioOferta: 50m, activa: false);                                                          // Inactiva
            var sut = Sut(ctx);

            var ofertas = await sut.ObtenerTodasAsync();

            Assert.Equal(4, ofertas.Count);
            Assert.All(ofertas, o => Assert.Equal(producto.ProductoId, o.ProductoId));
            Assert.Equal("Vigente", ofertas.Single(o => o.PrecioOferta == 80m).Estado);
            Assert.Equal("Futura", ofertas.Single(o => o.PrecioOferta == 70m).Estado);
            Assert.Equal("Expirada", ofertas.Single(o => o.PrecioOferta == 60m).Estado);
            Assert.Equal("Inactiva", ofertas.Single(o => o.PrecioOferta == 50m).Estado);
            Assert.All(ofertas, o => Assert.Equal(100m, o.PrecioLista));
            Assert.All(ofertas, o => Assert.Equal(producto.Nombre, o.NombreProducto));
        }

        [Fact]
        public async Task ObtenerTodas_SinOfertas_RetornaListaVacia()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var sut = Sut(ctx);

            var ofertas = await sut.ObtenerTodasAsync();

            Assert.Empty(ofertas);
        }
    }
}

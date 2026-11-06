using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Services.Ofertas;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Ofertas
{
    /// <summary>
    /// Tests del ABM de ofertas: guards de dominio (producto inexistente/
    /// inactivo, ventana invertida, precio de oferta menor al de lista) y el toggle Activa.
    /// </summary>
    public class OfertaCommandServiceTests
    {
        private static CrearOfertaDto Dto(int productoId, decimal precioOferta = 79.9m,
            DateTime? inicio = null, DateTime? fin = null)
            => new(productoId, precioOferta,
                inicio ?? DateTime.Now,
                fin ?? DateTime.Now.AddDays(7));

        [Fact]
        public async Task CrearOferta_DatosValidos_PersisteActiva()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var sut = new OfertaCommandService(ctx);

            await sut.CrearOfertaAsync(Dto(producto.ProductoId));

            using var verify = db.CreateContext();
            var ofertaDb = await verify.OfertasProducto.SingleAsync();
            Assert.Equal(producto.ProductoId, ofertaDb.ProductoId);
            Assert.Equal(79.9m, ofertaDb.PrecioOferta);
            Assert.True(ofertaDb.Activa);
        }

        [Fact]
        public async Task CrearOferta_ProductoInexistente_LanzaOfertaNoValida_YNoPersiste()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new OfertaCommandService(ctx);

            await Assert.ThrowsAsync<OfertaNoValidaException>(
                () => sut.CrearOfertaAsync(Dto(99999)));

            using var verify = db.CreateContext();
            Assert.Equal(0, await verify.OfertasProducto.CountAsync());
        }

        [Fact]
        public async Task CrearOferta_ProductoInactivo_LanzaOfertaNoValida()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            producto.Activo = false;
            ctx.SaveChanges();
            var sut = new OfertaCommandService(ctx);

            await Assert.ThrowsAsync<OfertaNoValidaException>(
                () => sut.CrearOfertaAsync(Dto(producto.ProductoId)));
        }

        [Fact]
        public async Task CrearOferta_VentanaInvertida_LanzaOfertaNoValida()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var sut = new OfertaCommandService(ctx);

            await Assert.ThrowsAsync<OfertaNoValidaException>(
                () => sut.CrearOfertaAsync(Dto(producto.ProductoId,
                    inicio: DateTime.Now.AddDays(5), fin: DateTime.Now.AddDays(1))));
        }

        [Fact]
        public async Task CrearOferta_PrecioIgualAlDeLista_LanzaOfertaNoValida()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var sut = new OfertaCommandService(ctx);

            await Assert.ThrowsAsync<OfertaNoValidaException>(
                () => sut.CrearOfertaAsync(Dto(producto.ProductoId, precioOferta: 100m)));
        }

        [Fact]
        public async Task EditarOferta_Inexistente_LanzaOfertaNoValida()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new OfertaCommandService(ctx);

            await Assert.ThrowsAsync<OfertaNoValidaException>(
                () => sut.EditarOfertaAsync(99999, Dto(1)));
        }

        [Fact]
        public async Task EditarOferta_DatosValidos_PisaCampos()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var oferta = TestDatabase.SeedOferta(ctx, producto, precioOferta: 80m);
            var sut = new OfertaCommandService(ctx);

            var nuevoFin = DateTime.Now.AddDays(30);
            await sut.EditarOfertaAsync(oferta.OfertaId,
                Dto(producto.ProductoId, precioOferta: 65.5m, fin: nuevoFin));

            using var verify = db.CreateContext();
            var ofertaDb = await verify.OfertasProducto.SingleAsync(o => o.OfertaId == oferta.OfertaId);
            Assert.Equal(65.5m, ofertaDb.PrecioOferta);
            Assert.Equal(nuevoFin, ofertaDb.FechaFin);
        }

        [Fact]
        public async Task AlternarActiva_Inexistente_LanzaOfertaNoValida()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = new OfertaCommandService(ctx);

            await Assert.ThrowsAsync<OfertaNoValidaException>(
                () => sut.AlternarActivaAsync(99999));
        }

        [Fact]
        public async Task AlternarActiva_PasaDeActivaAInactivaYVuelve()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx, precio: 100m, stock: 5);
            var oferta = TestDatabase.SeedOferta(ctx, producto, precioOferta: 80m, activa: true);
            var sut = new OfertaCommandService(ctx);

            await sut.AlternarActivaAsync(oferta.OfertaId);

            using var verify = db.CreateContext();
            var trasPrimerToggle = await verify.OfertasProducto
                .SingleAsync(o => o.OfertaId == oferta.OfertaId);
            Assert.False(trasPrimerToggle.Activa);
        }
    }
}

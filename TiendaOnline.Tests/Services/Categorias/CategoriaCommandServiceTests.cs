using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.Categorias
{
    /// <summary>
    /// Reglas de la edición de categorías (reactivada 14/11): el nombre y el
    /// padre viajan juntos y el guard de jerarquía corre ANTES de guardar — un
    /// bucle rechaza la edición completa (no queda un nombre nuevo bajo el
    /// padre viejo, ni media categoría reinsertada en el árbol).
    /// </summary>
    public class CategoriaCommandServiceTests
    {
        private static void Colgar(TiendaContext ctx, int hijoId, int? padreId)
        {
            ctx.Categorias.First(c => c.CategoriaId == hijoId).CategoriaPadreId = padreId;
            ctx.SaveChanges();
        }

        [Fact]
        public async Task EditarCategoria_NombreYPadreNuevos_GuardaAmbos()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var padre = TestDatabase.SeedCategoria(ctx, "Padre");
            var categoria = TestDatabase.SeedCategoria(ctx, "Original");
            var sut = Sut.CategoriaCommand(ctx);

            await sut.EditarCategoriaAsync(categoria.CategoriaId, "Renombrada", padre.CategoriaId);

            using var verify = db.CreateContext();
            var editada = await verify.Categorias.FirstAsync(c => c.CategoriaId == categoria.CategoriaId);
            Assert.Equal("Renombrada", editada.Nombre);
            Assert.Equal(padre.CategoriaId, editada.CategoriaPadreId);
        }

        [Fact]
        public async Task EditarCategoria_SinPadre_QuedaRaiz()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var padre = TestDatabase.SeedCategoria(ctx, "Padre");
            var categoria = TestDatabase.SeedCategoria(ctx, "Original");
            Colgar(ctx, categoria.CategoriaId, padre.CategoriaId);
            var sut = Sut.CategoriaCommand(ctx);

            await sut.EditarCategoriaAsync(categoria.CategoriaId, "Original", null);

            using var verify = db.CreateContext();
            var editada = await verify.Categorias.FirstAsync(c => c.CategoriaId == categoria.CategoriaId);
            Assert.Null(editada.CategoriaPadreId);
        }

        [Fact]
        public async Task EditarCategoria_CategoriaInexistente_NoExplota()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var sut = Sut.CategoriaCommand(ctx);

            await sut.EditarCategoriaAsync(999, "Fantasma", null);
        }

        [Fact]
        public async Task EditarCategoria_PadreEsEllaMisma_LanzaYNoGuarda()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var categoria = TestDatabase.SeedCategoria(ctx, "Original");
            var sut = Sut.CategoriaCommand(ctx);

            await Assert.ThrowsAsync<CategoriaNoValidaException>(
                () => sut.EditarCategoriaAsync(categoria.CategoriaId, "Otro nombre", categoria.CategoriaId));

            using var verify = db.CreateContext();
            var intacta = await verify.Categorias.FirstAsync(c => c.CategoriaId == categoria.CategoriaId);
            Assert.Equal("Original", intacta.Nombre);
            Assert.Null(intacta.CategoriaPadreId);
        }

        [Fact]
        public async Task EditarCategoria_PadreEsSuDescendiente_LanzaYNoGuarda()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            // Abuela → hija → nieta: hacer que la abuela cuelgue de la nieta
            // crea el bucle (la abuela sería ancestro de sí misma).
            var abuela = TestDatabase.SeedCategoria(ctx, "Abuela");
            var hija = TestDatabase.SeedCategoria(ctx, "Hija");
            Colgar(ctx, hija.CategoriaId, abuela.CategoriaId);
            var nieta = TestDatabase.SeedCategoria(ctx, "Nieta");
            Colgar(ctx, nieta.CategoriaId, hija.CategoriaId);
            var sut = Sut.CategoriaCommand(ctx);

            await Assert.ThrowsAsync<CategoriaNoValidaException>(
                () => sut.EditarCategoriaAsync(abuela.CategoriaId, "Otro nombre", nieta.CategoriaId));

            using var verify = db.CreateContext();
            var intacta = await verify.Categorias.FirstAsync(c => c.CategoriaId == abuela.CategoriaId);
            Assert.Equal("Abuela", intacta.Nombre);
            Assert.Null(intacta.CategoriaPadreId);
        }

        [Fact]
        public async Task EditarCategoria_PadreHermanoEsValido_NoLanza()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var raiz = TestDatabase.SeedCategoria(ctx, "Raiz");
            var hija1 = TestDatabase.SeedCategoria(ctx, "Hija 1");
            var hija2 = TestDatabase.SeedCategoria(ctx, "Hija 2");
            Colgar(ctx, hija1.CategoriaId, raiz.CategoriaId);
            Colgar(ctx, hija2.CategoriaId, raiz.CategoriaId);
            var sut = Sut.CategoriaCommand(ctx);

            // Mover hija1 bajo hija2 (su hermana) es un árbol válido, no un bucle.
            await sut.EditarCategoriaAsync(hija1.CategoriaId, "Hija 1", hija2.CategoriaId);

            using var verify = db.CreateContext();
            var movida = await verify.Categorias.FirstAsync(c => c.CategoriaId == hija1.CategoriaId);
            Assert.Equal(hija2.CategoriaId, movida.CategoriaPadreId);
        }
    }
}

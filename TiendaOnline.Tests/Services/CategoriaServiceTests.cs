using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Models;
using TiendaOnline.Services;
using Xunit;

namespace TiendaOnline.Tests.Services
{
    public class CategoriaServiceTests : IDisposable
    {
        private readonly TiendaContext _db;
        private readonly CategoriaService _service;

        public CategoriaServiceTests()
        {
            var options = new DbContextOptionsBuilder<TiendaContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _db = new TiendaContext(options);
            _service = new CategoriaService(_db);
        }

        #region Tests de Jerarquía y Bucles

        [Fact]
        public async Task VerificarSiCausaBucleAsync_DebeDetectarReferenciaCircular()
        {
            // ARRANGE
            // Estructura: Electrónica (1) -> Celulares (2)
            var catPadre = new Categoria { CategoriaId = 1, Nombre = "Electrónica" };
            var catHijo = new Categoria { CategoriaId = 2, Nombre = "Celulares", CategoriaPadreId = 1 };
            _db.Categorias.AddRange(catPadre, catHijo);
            await _db.SaveChangesAsync();

            // ACT
            // Intentamos que "Electrónica" sea hijo de "Celulares" (Bucle!)
            bool causaBucle = await _service.VerificarSiCausaBucleAsync(1, 2);

            // ASSERT
            Assert.True(causaBucle);
        }

        [Fact]
        public async Task CambiarCategoriaPadre_DebeLanzarExcepcion_SiHayBucle()
        {
            // ARRANGE
            var cat1 = new Categoria { CategoriaId = 1, Nombre = "A" };
            _db.Categorias.Add(cat1);
            await _db.SaveChangesAsync();

            // ACT & ASSERT
            // No puede ser hija de sí misma
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CambiarCategoriaPadre(1, 1));
        }

        #endregion

        #region Tests de Eliminación y Reglas de Negocio

        [Fact]
        public async Task EliminarCategoriaAsync_DebeRetornarFalso_SiTieneProductos()
        {
            // ARRANGE
            var cat = new Categoria { CategoriaId = 1, Nombre = "Hardware" };
            var prod = new Producto { ProductoId = 1, Nombre = "Mouse", Descripcion= "Descripcion para mouse" ,CategoriaId = 1 };
            _db.Categorias.Add(cat);
            _db.Productos.Add(prod);
            await _db.SaveChangesAsync();

            // ACT
            bool eliminado = await _service.EliminarCategoriaAsync(1);

            // ASSERT
            Assert.False(eliminado); // No debe dejar borrar si tiene productos vinculados
            var existe = await _db.Categorias.AnyAsync(c => c.CategoriaId == 1);
            Assert.True(existe);
        }

        [Fact]
        public async Task ExisteNombreAsync_DebeSerInsensibleAMayusculas()
        {
            // ARRANGE
            _db.Categorias.Add(new Categoria { Nombre = "HOGAR" });
            await _db.SaveChangesAsync();

            // ACT
            bool existe = await _service.ExisteNombreAsync("hogar");

            // ASSERT
            Assert.True(existe); // El servicio usa .ToLower()
        }

        #endregion

        #region Tests de Consultas

        [Fact]
        public async Task ObtenerCategoriasRaizAsync_DebeTraerSoloLasQueNoTienenPadre()
        {
            // ARRANGE
            _db.Categorias.AddRange(
                new Categoria { CategoriaId = 1, Nombre = "Raiz 1", CategoriaPadreId = null },
                new Categoria { CategoriaId = 2, Nombre = "Hijo", CategoriaPadreId = 1 }
            );
            await _db.SaveChangesAsync();

            // ACT
            var raices = await _service.ObtenerCategoriasRaizAsync();

            // ASSERT
            Assert.Single(raices);
            Assert.Equal("Raiz 1", raices.First().Nombre);
        }

        #endregion

        public void Dispose()
        {
            _db.Database.EnsureDeleted();
            _db.Dispose();
        }
    }
}
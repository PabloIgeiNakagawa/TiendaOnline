using System.Threading.Tasks;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Infrastructure.Services.Reportes;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.Reportes
{
    /// <summary>
    /// El dashboard de reportes armado contra datos ya vividos: las métricas se
    /// calculan en memoria con entidades de contexto fresco, así que un Include
    /// faltante en el fetch no lo tapa nada (en producción cada request arranca
    /// sin nada tracked).
    /// </summary>
    public class ReportesTests
    {
        private readonly TestDatabase _db = new();

        [Fact]
        public async Task ObtenerStockInmovilizadoAsync_ContextoFresco_NombraLaCategoria()
        {
            // Seed en un contexto y lectura en OTRO: si el fetch del armado no
            // Include a la categoría, p.Categoria queda NULL y p.Categoria.Nombre
            // tira NullReferenceException (fue el 500 del dashboard en producción).
            using (var seed = _db.CreateContext())
            {
                TestDatabase.SeedProducto(seed, stock: 5);
            }

            using var context = _db.CreateContext();
            var datos = await new ReportesService(context).ObtenerDatosAsync();

            var stock = Assert.Single(datos.StockInmovilizado);
            Assert.Equal(5, stock.Stock);
            Assert.False(string.IsNullOrWhiteSpace(stock.Categoria));
            Assert.Equal("Categoria Test", stock.Categoria);
        }
    }
}

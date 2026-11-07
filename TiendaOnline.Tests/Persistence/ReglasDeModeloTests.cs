using Microsoft.EntityFrameworkCore;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Persistence
{
    /// <summary>
    /// Guardas sobre el modelo de datos: la shape de la FK Producto→Categoría es una
    /// regla de negocio (bajar una categoría no puede arrastrar productos) y la
    /// convención de EF la dejaba en Cascade. El test fija la decisión contra
    /// regresiones: si alguien saca la config explícita, queda Cascade y el test avisa.
    /// </summary>
    public class ReglasDeModeloTests
    {
        private readonly TestDatabase _db = new();

        [Fact]
        public void Producto_Categoria_FKEsObligatoriaYRestrictAlBorrar()
        {
            using var context = _db.CreateContext();

            var producto = context.Model.FindEntityType(typeof(Producto))!;
            var fk = Assert.Single(producto.GetForeignKeys(),
                f => f.PrincipalEntityType.ClrType == typeof(Categoria));

            // Obligatoria: todo producto pertenece a una categoría.
            Assert.True(fk.IsRequired);

            // Restrict: la base rechaza borrar una categoría con productos (la baja
            // comercial de categorías es lógica; el borrado físico solo aplica a filas sin hijos).
            Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
        }
    }
}

using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Services.Carritos;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.Carritos
{
    /// <summary>
    /// Persistencia del carrito: el storage de BD del usuario logueado, el routing
    /// (logueado → BD / anónimo → session) y la fusión del carrito de anónimo al
    /// loguear. La session se mockea a nivel ISession (TryGetValue/Set/Remove, las
    /// extensiones de trabajo sobre eso).
    /// </summary>
    public class CarritoPersistenciaTests : IDisposable
    {
        private readonly TestDatabase _db = new();

        public void Dispose()
        {
            _db.Dispose();
        }

        private static ItemCarrito Item(int productoId, int cantidad)
            => new ItemCarrito { ProductoId = productoId, Nombre = $"Producto {productoId}", Cantidad = cantidad };

        private static Mock<IUserSessionService> Sesion(int? usuarioId)
        {
            var sesion = new Mock<IUserSessionService>();
            sesion.Setup(x => x.GetUserId()).Returns(usuarioId);
            return sesion;
        }

        // Accesor + ISession: TryGetValue entrega el JSON guardado si lo hay (con
        // It.Ref para el caso vacío: sin datos, una constante non-null en el out
        // param haría creer a las extensiones que hay "" y revienta el Deserialize),
        // Set captura lo escrito y Remove lo borra (las extensiones trabajan sobre esto).
        private static (Mock<IHttpContextAccessor> Accesor, Mock<ISession> Sesion) SesionHttp(string? jsonCarrito)
        {
            var sesion = new Mock<ISession>();

            if (jsonCarrito is null)
            {
                sesion.Setup(s => s.TryGetValue(It.IsAny<string>(), out It.Ref<byte[]>.IsAny))
                      .Returns(false);
            }
            else
            {
                byte[] bytes = Encoding.UTF8.GetBytes(jsonCarrito);
                sesion.Setup(s => s.TryGetValue(It.IsAny<string>(), out bytes))
                      .Returns(true);
            }

            var http = new Mock<HttpContext>();
            http.SetupGet(x => x.Session).Returns(sesion.Object);

            var accesor = new Mock<IHttpContextAccessor>();
            accesor.SetupGet(a => a.HttpContext).Returns(http.Object);

            return (accesor, sesion);
        }

        // ---------------------------------------------------------------------
        // DbCarritoStorage
        // ---------------------------------------------------------------------

        [Fact]
        public async Task DbStorage_GuardarYObtener_PersisteLosItems()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-guardar@test.local");
            using var context = _db.CreateContext(usuario.UsuarioId);
            var storage = new DbCarritoStorage(context, Sesion(usuario.UsuarioId).Object);

            await storage.GuardarAsync(new List<ItemCarrito> { Item(1, 2), Item(2, 1) });

            var obtenidos = await storage.ObtenerAsync();

            Assert.Equal(2, obtenidos.Count);
            Assert.Contains(obtenidos, i => i.ProductoId == 1 && i.Cantidad == 2);
            Assert.Contains(obtenidos, i => i.ProductoId == 2 && i.Cantidad == 1);
        }

        [Fact]
        public async Task DbStorage_GuardarDosVeces_HaceUpsertSinDuplicarFila()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-upsert@test.local");
            using var context = _db.CreateContext(usuario.UsuarioId);
            var storage = new DbCarritoStorage(context, Sesion(usuario.UsuarioId).Object);

            await storage.GuardarAsync(new List<ItemCarrito> { Item(1, 1) });
            await storage.GuardarAsync(new List<ItemCarrito> { Item(1, 3), Item(2, 1) });

            using var verificador = _db.CreateContext(usuario.UsuarioId);
            var filas = await verificador.CarritosUsuarios.ToListAsync();

            var fila = Assert.Single(filas);
            Assert.Contains("\"Cantidad\":3", fila.Contenido);
        }

        [Fact]
        public async Task DbStorage_ObtenerSinFila_VuelveListaVacia()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-vacio@test.local");
            using var context = _db.CreateContext(usuario.UsuarioId);
            var storage = new DbCarritoStorage(context, Sesion(usuario.UsuarioId).Object);

            var obtenidos = await storage.ObtenerAsync();

            Assert.Empty(obtenidos);
        }

        [Fact]
        public async Task DbStorage_Limpiar_BorraLaFilaGuardada()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-limpiar@test.local");
            using var context = _db.CreateContext(usuario.UsuarioId);
            var storage = new DbCarritoStorage(context, Sesion(usuario.UsuarioId).Object);
            await storage.GuardarAsync(new List<ItemCarrito> { Item(1, 1) });

            await storage.LimpiarAsync();

            Assert.Empty(await storage.ObtenerAsync());
            using var verificador = _db.CreateContext(usuario.UsuarioId);
            Assert.Equal(0, await verificador.CarritosUsuarios.CountAsync());
        }

        // ---------------------------------------------------------------------
        // RoutingCarritoStorage
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Routing_Logueado_GuardaEnBdYNoTocaLaSesion()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-routing-logueado@test.local");
            var sesionUsuario = Sesion(usuario.UsuarioId);
            using var context = _db.CreateContext(usuario.UsuarioId);
            var (accesor, sesionHttp) = SesionHttp(null);

            var router = new RoutingCarritoStorage(
                sesionUsuario.Object,
                new SessionCarritoStorage(accesor.Object),
                new DbCarritoStorage(context, sesionUsuario.Object));

            await router.GuardarAsync(new List<ItemCarrito> { Item(3, 5) });

            using var verificador = _db.CreateContext(usuario.UsuarioId);
            Assert.Equal(1, await verificador.CarritosUsuarios.CountAsync());
            sesionHttp.Verify(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task Routing_Anonimo_GuardaYLeerEnSessionSinTocarLaBd()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-routing-anon@test.local");
            var sesionUsuario = Sesion(null);
            using var context = _db.CreateContext(usuario.UsuarioId);
            byte[]? guardado = null;
            var (accesor, sesionHttp) = SesionHttp(null);
            sesionHttp.Setup(s => s.Set(It.IsAny<string>(), It.IsAny<byte[]>()))
                      .Callback<string, byte[]>((key, valor) => guardado = valor);

            var router = new RoutingCarritoStorage(
                sesionUsuario.Object,
                new SessionCarritoStorage(accesor.Object),
                new DbCarritoStorage(context, sesionUsuario.Object));

            await router.GuardarAsync(new List<ItemCarrito> { Item(3, 5) });

            // El item terminó en la session y la BD de carritos sigue sin fila para el usuario.
            var json = Encoding.UTF8.GetString(guardado!);
            Assert.Contains("\"ProductoId\":3", json);
            using var verificador = _db.CreateContext(usuario.UsuarioId);
            Assert.Equal(0, await verificador.CarritosUsuarios.CountAsync());
        }

        // ---------------------------------------------------------------------
        // CarritoSesionMigrador (fusión al loguear)
        // ---------------------------------------------------------------------

        [Fact]
        public async Task Fusionar_ConCarritoDeSesion_LoMueveALaBdYLimpiaLaSesion()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-fusion@test.local");
            var sesionHttp = SesionHttp(
                System.Text.Json.JsonSerializer.Serialize(new List<ItemCarrito> { Item(5, 2) }));

            using var context = _db.CreateContext(usuario.UsuarioId);
            var migrador = new CarritoSesionMigrador(new SessionCarritoStorage(sesionHttp.Accesor.Object), context);

            var fusiono = await migrador.FusionarConCarritoGuardadoAsync(usuario.UsuarioId);

            Assert.True(fusiono);
            sesionHttp.Sesion.Verify(s => s.Remove(It.IsAny<string>()), Times.Once);

            var storage = new DbCarritoStorage(context, Sesion(usuario.UsuarioId).Object);
            var obtenidos = await storage.ObtenerAsync();
            var item = Assert.Single(obtenidos);
            Assert.Equal(5, item.ProductoId);
            Assert.Equal(2, item.Cantidad);
        }

        [Fact]
        public async Task Fusionar_ConCarritoGuardadoPrevio_SumaCantidadPorProducto()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-fusion-suma@test.local");

            // El carrito guardado ya tenía: producto 5 x1 y producto 7 x1.
            using var context = _db.CreateContext(usuario.UsuarioId);
            await new DbCarritoStorage(context, Sesion(usuario.UsuarioId).Object)
                .GuardarAsync(new List<ItemCarrito> { Item(5, 1), Item(7, 1) });

            // La sesión trae el mismo producto 5 (x2) más uno nuevo.
            var sesionHttp = SesionHttp(
                System.Text.Json.JsonSerializer.Serialize(new List<ItemCarrito> { Item(5, 2), Item(9, 4) }));
            var migrador = new CarritoSesionMigrador(new SessionCarritoStorage(sesionHttp.Accesor.Object), context);

            var fusiono = await migrador.FusionarConCarritoGuardadoAsync(usuario.UsuarioId);

            Assert.True(fusiono);
            var obtenidos = await new DbCarritoStorage(context, Sesion(usuario.UsuarioId).Object).ObtenerAsync();
            Assert.Equal(3, obtenidos.Count);
            Assert.Contains(obtenidos, i => i.ProductoId == 5 && i.Cantidad == 3); // 1 (BD) + 2 (sesión)
            Assert.Contains(obtenidos, i => i.ProductoId == 7 && i.Cantidad == 1);
            Assert.Contains(obtenidos, i => i.ProductoId == 9 && i.Cantidad == 4);
        }

        [Fact]
        public async Task Fusionar_SesionVacia_DevuelveFalseYNoCreaFila()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-fusion-vacio@test.local");
            var sesionHttp = SesionHttp(null);
            using var context = _db.CreateContext(usuario.UsuarioId);
            var migrador = new CarritoSesionMigrador(new SessionCarritoStorage(sesionHttp.Accesor.Object), context);

            var fusiono = await migrador.FusionarConCarritoGuardadoAsync(usuario.UsuarioId);

            Assert.False(fusiono);
            sesionHttp.Sesion.Verify(s => s.Remove(It.IsAny<string>()), Times.Never);
            using var verificador = _db.CreateContext(usuario.UsuarioId);
            Assert.Equal(0, await verificador.CarritosUsuarios.CountAsync());
        }

        [Fact]
        public async Task Fusionar_IgnoraItemsConCantidadCero_YFusionaElResto()
        {
            var usuario = TestDatabase.SeedUsuario(_db.CreateContext(), "carrito-fusion-cero@test.local");
            var sesionHttp = SesionHttp(
                System.Text.Json.JsonSerializer.Serialize(new List<ItemCarrito> { Item(9, 0), Item(2, 1) }));
            using var context = _db.CreateContext(usuario.UsuarioId);
            var migrador = new CarritoSesionMigrador(new SessionCarritoStorage(sesionHttp.Accesor.Object), context);

            var fusiono = await migrador.FusionarConCarritoGuardadoAsync(usuario.UsuarioId);

            Assert.True(fusiono);
            var obtenidos = await new DbCarritoStorage(context, Sesion(usuario.UsuarioId).Object).ObtenerAsync();
            var item = Assert.Single(obtenidos);
            Assert.Equal(2, item.ProductoId);
            Assert.Equal(1, item.Cantidad);
        }
    }
}

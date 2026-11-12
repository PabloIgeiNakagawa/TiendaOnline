using Hangfire;
using Hangfire.InMemory;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Tests.TestSupport
{
    /// <summary>
    /// Contexto de EF para tests: igual que TiendaContext pero con un default de
    /// fecha compatible con SQLite (el default GETDATE() es T-SQL y rompe el
    /// CREATE TABLE del provider de SQLite; Fecha ya se setea en código siempre).
    /// </summary>
    public class TestTiendaContext : TiendaContext
    {
        public TestTiendaContext(DbContextOptions<TiendaContext> options, IUserSessionService userSession)
            : base(options, userSession)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MovimientoStock>()
                .Property(m => m.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }

    /// <summary>
    /// Database de test sobre SQLite in-memory con TiendaContext real.
    /// Se eligió SQLite (y no EF InMemory) porque el flujo de reserva de stock
    /// usa ExecuteUpdateAsync, que el provider InMemory no soporta.
    /// La conexión queda abierta mientras exista la instancia: un :memory: sin
    /// conexión viva se borra.
    /// </summary>
    public sealed class TestDatabase : IDisposable
    {
        private readonly SqliteConnection _connection;

        static TestDatabase()
        {
            // Los servicios encolan emails vía BackgroundJob (Hangfire estático).
            // Sin storage configurado tira "JobStorage.Current has not been initialized".
            JobStorage.Current = new InMemoryStorage();
        }

        public TestDatabase()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        /// <summary>
        /// Crea otro contexto sobre la MISMA database (misma conexión).
        /// Usalo para verificar estado: el change tracker de un contexto ya usado
        /// puede tener valores stale (ej. stock cambiado con ExecuteUpdateAsync).
        /// </summary>
        public TestTiendaContext CreateContext(int? userId = null)
        {
            var session = new Mock<IUserSessionService>();
            session.Setup(x => x.GetUserId()).Returns(userId);

            var options = new DbContextOptionsBuilder<TiendaContext>()
                .UseSqlite(_connection)
                .Options;

            return new TestTiendaContext(options, session.Object);
        }

        /// <summary>Conexión viva compartida: los hosts de integración registran la DB sobre ella.</summary>
        public SqliteConnection Connection => _connection;

        public void Dispose()
        {
            _connection.Dispose();
        }

        // ------------------------------------------------------------------
        // Helpers de seed
        // ------------------------------------------------------------------

        public static Categoria SeedCategoria(TiendaContext ctx, string nombre = "Categoria Test")
        {
            var categoria = new Categoria { Nombre = nombre };
            ctx.Categorias.Add(categoria);
            ctx.SaveChanges();
            return categoria;
        }

        public static Usuario SeedUsuario(TiendaContext ctx, string? email = null, Rol rol = Rol.Usuario,
            string? telefono = null)
        {
            var usuario = new Usuario
            {
                Nombre = "Test",
                Apellido = "User",
                Email = email ?? $"user-{Guid.NewGuid():N}@test.local",
                Contrasena = "hash-fake",
                Rol = rol,
                Telefono = telefono
            };
            ctx.Usuarios.Add(usuario);
            ctx.SaveChanges();
            return usuario;
        }

        public static MetodoDePago SeedMetodoDePago(TiendaContext ctx, string nombre = "Mercado Pago")
        {
            var metodo = new MetodoDePago { Nombre = nombre };
            ctx.Set<MetodoDePago>().Add(metodo);
            ctx.SaveChanges();
            return metodo;
        }

        public static Producto SeedProducto(TiendaContext ctx, decimal precio = 100m, int stock = 5,
            string? nombre = null)
        {
            var categoria = SeedCategoria(ctx);
            var producto = new Producto
            {
                Nombre = nombre ?? ("Prod-" + Guid.NewGuid().ToString("N")[..20]),
                Descripcion = "Producto de test",
                Precio = precio,
                Stock = stock,
                ImagenUrl = "https://img.test/producto.png",
                CategoriaId = categoria.CategoriaId
            };
            ctx.Productos.Add(producto);
            ctx.SaveChanges();
            return producto;
        }

        public static OfertaProducto SeedOferta(
            TiendaContext ctx,
            Producto producto,
            decimal precioOferta,
            DateTime? inicio = null,
            DateTime? fin = null,
            bool activa = true)
        {
            var oferta = new OfertaProducto
            {
                ProductoId = producto.ProductoId,
                PrecioOferta = precioOferta,
                FechaInicio = inicio ?? DateTime.Now.AddHours(-1),
                FechaFin = fin ?? DateTime.Now.AddDays(7),
                Activa = activa
            };
            ctx.OfertasProducto.Add(oferta);
            ctx.SaveChanges();
            return oferta;
        }

        public static ZonaEnvio SeedZonaEnvio(
            TiendaContext ctx,
            string provincia,
            decimal costo = 500m,
            bool activa = true)
        {
            var zona = new ZonaEnvio
            {
                Provincia = provincia,
                Costo = costo,
                Activa = activa
            };
            ctx.ZonasEnvio.Add(zona);
            ctx.SaveChanges();
            return zona;
        }

        public static Pedido SeedPedido(
            TiendaContext ctx,
            Usuario usuario,
            Producto producto,
            int cantidad,
            decimal? precioUnitario = null,
            EstadoPedido estado = EstadoPedido.Nuevo,
            EstadoPago estadoPago = EstadoPago.Pendiente,
            DateTime? fechaPedido = null,
            DateTime? fechaEnPreparacion = null,
            DateTime? fechaEnvio = null,
            DateTime? fechaEntrega = null,
            decimal costoEnvio = 0,
            string? transaccionId = null,
            int? metodoDePagoId = null,
            string? comprobanteUrl = null)
        {
            var pedido = new Pedido
            {
                UsuarioId = usuario.UsuarioId,
                MetodoDePagoId = metodoDePagoId ?? SeedMetodoDePago(ctx).MetodoDePagoId,
                EsEnvioADomicilio = false,
                Estado = estado,
                EstadoPago = estadoPago,
                FechaPedido = fechaPedido ?? DateTime.Now,
                FechaEnPreparacion = fechaEnPreparacion ?? (estado == EstadoPedido.EnPreparacion ? DateTime.Now : null),
                FechaEnvio = fechaEnvio ?? (estado == EstadoPedido.Enviado ? DateTime.Now : null),
                FechaEntrega = fechaEntrega ?? (estado == EstadoPedido.Entregado ? DateTime.Now : null),
                FechaCancelado = estado == EstadoPedido.Cancelado ? DateTime.Now : null,
                CostoEnvio = costoEnvio,
                TransaccionPagoId = transaccionId,
                ComprobanteUrl = comprobanteUrl
            };

            pedido.DetallesPedido.Add(new DetallePedido
            {
                ProductoId = producto.ProductoId,
                Cantidad = cantidad,
                PrecioUnitario = precioUnitario ?? producto.Precio,
                Producto = producto
            });

            ctx.Pedidos.Add(pedido);
            ctx.SaveChanges();
            return pedido;
        }
    }
}

using Hangfire;
using Hangfire.InMemory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using TiendaOnline.Application.Auth;
using TiendaOnline.Domain.Entities;
using TiendaOnline.E2E.Host;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.E2E
{
    /// <summary>
    /// Colección E2E: reúne un solo host para todos los smoke tests (app corriendo de
    /// verdad + un browser Chromium). La serialización evita choques por logs y
    /// DataProtection de arranques concurrentes, como la colección "Integracion".
    /// </summary>
    [CollectionDefinition("E2E")]
    public sealed class ColeccionE2E : ICollectionFixture<E2EFixture>
    {
    }

    /// <summary>
    /// Fixture de los smoke tests: la app arranca con una base sembrada (settings, zonas,
    /// métodos de pago, un producto) y dos usuarios creados por el propio sistema
    /// (IAuthService), así el login de cada smoke test recorre el mismo camino hasheado
    /// que la producción. El browser se lanza una vez; cada test abre su propio
    /// BrowserContext (cookies aisladas: un usuario por smoke test).
    /// </summary>
    public sealed class E2EFixture : IAsyncLifetime
    {
        public const string Password = "E2e-Admin-2026!";
        public const string EmailAdmin = "admin-e2e@tienda.local";
        public const string EmailClienteSeed = "cliente-e2e@tienda.local";
        public const string NombreProductoTienda = "Router E2E";

        public E2EWebApplicationFactory App { get; } = new();

        public string BaseUrl => App.BaseUrl;

        private IPlaywright? _playwright;
        private IBrowser? _browser;

        // El mismo flaky que ya domamos en TestApp: BackgroundJob (emails) usa el
        // JobStorage.Current global, y AddHangfire solo lo setea cuando arranca el
        // server (acá ese hosted service está fuera). Se repone al morir el fixture.
        // El encolado de emails vía BackgroundJob (estático de Hangfire) requiere
        // un storage ya seteado en el Current global: AddHangfire solo lo setea
        // cuando arranca el server, y ese hosted service está fuera del host de
        // test. El static ctor corre antes que los inicializadores de campo del
        // factory (que leen Current para capturarlo) y antes de que la app encole.
        static E2EFixture()
        {
            JobStorage.Current = new InMemoryStorage();
        }

        private readonly JobStorage _storagePrevio = JobStorage.Current;


        public async Task InitializeAsync()
        {
            using var scope = App.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TiendaContext>();
            context.Database.EnsureCreated();

            DbInitializer.SeedSettings(context);
            DbInitializer.SeedZonasEnvio(context);

            // Métodos de pago: el checkout arma el pedido con el id elegido
            // (la seed del sistema no los cria; en producción existen desde el inicio).
            var metodos = context.Set<MetodoDePago>();
            if (!metodos.Any())
            {
                metodos.Add(new MetodoDePago { Nombre = "Mercado Pago" });
                metodos.Add(new MetodoDePago { Nombre = "Transferencia" });
                metodos.Add(new MetodoDePago { Nombre = "Efectivo" });
                context.SaveChanges();
            }

            // Un solo producto en la tienda: el "primer botón Agregar al carrito"
            // de /Productos es determinísticamente este. La categoría se guarda
            // primero: el id real hace falta en la FK del producto.
            var categoria = new Categoria { Nombre = "Redes" };
            context.Categorias.Add(categoria);
            context.SaveChanges();
            context.Productos.Add(new Producto
            {
                Nombre = NombreProductoTienda,
                Descripcion = "Producto cargado por el fixture de E2E",
                Precio = 1000m,
                Stock = 5,
                ImagenUrl = "https://img.e2e.test/producto.png",
                CategoriaId = categoria.CategoriaId
            });
            context.SaveChanges();

            // Usuarios con password hasheada por el hasher real de la app.
            var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
            await auth.RegisterAsync(new RegisterDto
            {
                Nombre = "Admin",
                Apellido = "E2E",
                Email = EmailAdmin,
                Telefono = "3510001111",
                FechaNacimiento = new DateTime(1990, 1, 1),
                Contrasena = Password,
                RolId = (int)Rol.Administrador
            }, permitirRolPersonalizado: true);

            await auth.RegisterAsync(new RegisterDto
            {
                Nombre = "Cliente",
                Apellido = "E2E",
                Email = EmailClienteSeed,
                Telefono = "3510002222",
                FechaNacimiento = new DateTime(1995, 5, 5),
                Contrasena = Password
            });

            _playwright = await Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        }

        /// <summary>BrowserContext nuevo: cookies limpias, un usuario distinto por smoke test.</summary>
        public async Task<IBrowserContext> CrearContextoAsync()
        {
            var browser = _browser ?? throw new InvalidOperationException("El browser no está lanzado.");
            return await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
                Locale = "es-AR",
            });
        }

        /// <summary>Login por el formulario real de la tienda.</summary>
        public async Task LoginAsync(IPage page, string email)
        {
            await page.GotoAsync($"{BaseUrl}/Accounts/Login");
            await page.FillAsync("#email", email);
            await page.FillAsync("#password", Password);
            await page.ClickAsync("#loginBtn");
        }

        /// <summary>Leer/escribir la DB real que la app está sirviendo (verification de efectos).</summary>
        public T ConDb<T>(Func<TiendaContext, T> accion)
        {
            using var scope = App.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TiendaContext>();
            return accion(context);
        }

        public void ConDb(Action<TiendaContext> accion)
        {
            using var scope = App.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TiendaContext>();
            accion(context);
        }

        public async Task DisposeAsync()
        {
            if (_browser != null) await _browser.DisposeAsync();
            _playwright?.Dispose();
            await App.DisposeAsync();
            JobStorage.Current = _storagePrevio;
        }
    }
}

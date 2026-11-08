using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Hangfire;
using System.Security.Claims;
using System.Text.Encodings.Web;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// Esquema de autenticación de test: el principal viene de headers del request.
    /// Así cada test decide quién está "logueado" sin tocar cookies ni contraseñas.
    /// </summary>
    public class TestAuthHandlerOptions : AuthenticationSchemeOptions { }

    public class TestAuthHandler : AuthenticationHandler<TestAuthHandlerOptions>
    {
        public const string SchemeName = "Test";
        public const string UserIdHeader = "X-Test-User-Id";
        public const string RolesHeader = "X-Test-User-Roles";

        public TestAuthHandler(IOptionsMonitor<TestAuthHandlerOptions> options, ILoggerFactory logger, UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var userId = Request.Headers[UserIdHeader].ToString();
            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId) };
            foreach (var rol in Request.Headers[RolesHeader].ToString().Split(',').Where(r => r.Length > 0))
            {
                claims.Add(new Claim(ClaimTypes.Role, rol.Trim()));
            }

            var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    /// <summary>
    /// Host de la app real (Program.cs) para tests de integración: SQLite in-memory
    /// sobre la misma conexión que el seed de TestDatabase, esquema de auth por
    /// headers y todos los servicios con I/O externo reemplazados por mocks.
    /// El servidor de Hangfire no arranca en tests (sin hosted services); lo encolado
    /// vía BackgroundJob queda en el InMemoryStorage que TestDatabase ya configura.
    /// </summary>
    public sealed class TestApp : WebApplicationFactory<Program>
    {
        public Mock<IPaymentService> Payment { get; } = new();
        public Mock<IEmailService> Email { get; } = new();
        public Mock<IImagenService> Imagen { get; } = new();
        public Mock<ICreacionPedidoService> CreacionPedido { get; } = new();
        public Mock<IConfirmacionPagoService> ConfirmacionPago { get; } = new();
        public Mock<IGestionTransferenciaService> GestionTransferencia { get; } = new();
        public Mock<IEstadosPedidoService> EstadosPedido { get; } = new();
        public Mock<ICarritoService> Carrito { get; } = new();
        public Mock<IAppSettingsService> Settings { get; } = new();
        public Mock<IComprobantePdfService> Pdf { get; } = new();

        public TestDatabase Database { get; } = new();

        // AddHangfire pisa el JobStorage.Current global con el storage del host:
        // al desbotear la app la DI lo dispone (su Dispatcher queda muerto) y todo
        // encolado posterior desde tests unitarios revienta. Se captura el valor
        // de entrada y se repone cuando el host muere.
        private readonly JobStorage _jobStoragePrevio = JobStorage.Current;

        public TestApp()
        {
            // Default benigno: carrito vacío. Cada test ajusta lo que necesita.
            Carrito.Setup(x => x.ObtenerAsync()).ReturnsAsync(new List<ItemCarrito>());
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            JobStorage.Current = _jobStoragePrevio;
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            JobStorage.Current = _jobStoragePrevio;
        }

        /// <summary>Client autenticado como un usuario del test (sin redirect automático).</summary>
        public HttpClient CreateClientAs(int userId, string[]? roles = null)
            => CreateClient(useRedirect: false, userId: userId, roles: roles);

        /// <summary>Client anónimo (sin redirect automático: ideal para asserts de redirect).</summary>
        public HttpClient CreateAnonymousClient() => CreateClient(useRedirect: false);

        /// <summary>Corre una acción/anulación contra la DB real de la app (SQLite del host de test).</summary>
        public T WithDb<T>(Func<TiendaContext, T> action)
        {
            using var scope = Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<TiendaContext>();
            context.Database.EnsureCreated();
            return action(context);
        }

        /// <summary>
        /// Limpia setups e invocaciones de los mocks: los tests comparten fixture así que
        /// sin reset, los Verify de un test leen llamadas del test anterior (el orden entre
        /// tests no es el del archivo). Cada test re-stupea lo que necesita al arrancar.
        /// </summary>
        public void ResetMocks()
        {
            Payment.Reset();
            Email.Reset();
            Imagen.Reset();
            CreacionPedido.Reset();
            ConfirmacionPago.Reset();
            GestionTransferencia.Reset();
            EstadosPedido.Reset();
            Carrito.Reset();
            Settings.Reset();
            Pdf.Reset();

            Carrito.Setup(x => x.ObtenerAsync()).ReturnsAsync(new List<ItemCarrito>());
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureTestServices(services =>
            {
                // DB real: SQLite sobre la MISMA conexión viva de TestDatabase.
                services.RemoveAll(typeof(DbContextOptions<TiendaContext>));
                services.RemoveAll(typeof(TiendaContext));
                services.AddScoped<TiendaContext>(sp =>
                {
                    var session = sp.GetRequiredService<IUserSessionService>();
                    var options = new DbContextOptionsBuilder<TiendaContext>()
                        .UseSqlite(Database.Connection)
                        .Options;
                    return new TestTiendaContext(options, session);
                });

                // Sin hosted services: evitamos el daemon de Hangfire (su storage es SQL Server
                // de producción) para que el run de tests no dependa de la red.
                services.RemoveAll<IHostedService>();

                // El storage de Hangfire de producción es SQL Server (se materializa con el
                // primer uso del client API): en tests pasa a in-memory y nada habla con la red.
                services.AddHangfire(config => config.UseInMemoryStorage());

                // Autenticación de test: el default pasa a ser el esquema por headers.
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = TestAuthHandler.SchemeName;
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                }).AddScheme<TestAuthHandlerOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

                // Mocks de servicios externos (MercadoPago, Resend, Cloudinary) y de
                // servicios no enfocados por el test en curso: nadie habla con la red real.
                services.RemoveAll<IPaymentService>();
                services.AddSingleton(Payment.Object);
                services.RemoveAll<IEmailService>();
                services.AddSingleton(Email.Object);
                services.RemoveAll<IImagenService>();
                services.AddSingleton(Imagen.Object);
                services.RemoveAll<ICreacionPedidoService>();
                services.AddSingleton(CreacionPedido.Object);
                services.RemoveAll<IConfirmacionPagoService>();
                services.AddSingleton(ConfirmacionPago.Object);
                services.RemoveAll<IGestionTransferenciaService>();
                services.AddSingleton(GestionTransferencia.Object);
                services.RemoveAll<IEstadosPedidoService>();
                services.AddSingleton(EstadosPedido.Object);
                services.RemoveAll<ICarritoService>();
                services.AddSingleton(Carrito.Object);
                services.RemoveAll<IAppSettingsService>();
                services.AddSingleton(Settings.Object);
                services.RemoveAll<IComprobantePdfService>();
                services.AddSingleton(Pdf.Object);
            });
        }

        private HttpClient CreateClient(bool useRedirect, int? userId = null, string[]? roles = null)
        {
            var options = new WebApplicationFactoryClientOptions { AllowAutoRedirect = useRedirect };
            var client = CreateClient(options);
            if (userId != null)
            {
                client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.Value.ToString());
                if (roles != null && roles.Length > 0)
                {
                    client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(",", roles));
                }
            }
            return client;
        }
    }
}

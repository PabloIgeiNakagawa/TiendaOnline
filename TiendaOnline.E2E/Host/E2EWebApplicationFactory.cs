using Hangfire;
using Hangfire.InMemory;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.TestHost;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Payment;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.E2E.Host
{
    /// <summary>
    /// Host de la app REAL (Program.cs) con Kestrel escuchando en un puerto libre
    /// de loopback: el browser de Playwright necesita HTTP de verdad, no el
    /// TestServer in-process de los tests de integración. La base es SQLite
    /// in-memory sobre una conexión viva compartida; MP/Resend/Cloudinary van por
    /// fakes; y la autenticación es la real (cookie + password hasheada), porque
    /// el punto de los smoke tests es recorrer el login real del usuario.
    /// </summary>
    public sealed class E2EWebApplicationFactory : WebApplicationFactory<Program>
    {
        public E2EPagoFalso Pagos { get; } = new();
        public E2EEmailFalso Emails { get; } = new();
        public E2EImagenFalso Imagenes { get; } = new();

        public SqliteConnection Conexion { get; } = new("DataSource=:memory:");

        /// <summary>URL base del host vivo, ej: http://127.0.0.1:52844</summary>
        public string BaseUrl { get; private set; } = "";

        // Misma cura que TestApp: AddHangfire puede reemplazar el JobStorage.Current
        // global y al morir la app lo deja envenenado para los tests que corran
        // después en el mismo proceso. Se captura al construir (ya seteado por el
        // static ctor del fixture) y se repone al morir.
        private readonly JobStorage? _jobStoragePrevio;

        // El host vivo de Kestrel que sirve al browser: no es el que devuelve WAF
        // (ese es el de utilería con TestServer), así que lo guardo para pararlo.
        private IHost? _hostReal;

        public E2EWebApplicationFactory()
        {
            try
            {
                _jobStoragePrevio = JobStorage.Current;
            }
            catch (InvalidOperationException)
            {
                // Nunca se seteó: no hay nada que restaurar.
            }

            // Sin conexión viva, un :memory: se borra al soltarla: la abrimos
            // propia del fixture y EF arma el schema sobre la MISMA conexión.
            Conexion.Open();
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Ambiente Development: el boot NO corre las migraciones (son SQL Server
            // y explotan en SQLite; el schema lo arma el fixture con EnsureCreated),
            // no aplica HSTS/HttpsRedirection (el browser habla http local) ni
            // desvía los errores a /Home/Error (un 500 muestra la excepción cruda).
            builder.UseEnvironment("Development");

            builder.ConfigureTestServices(services =>
            {
                // DB real de la app: SQLite en memoria (pasa la conexión viva como TestApp).
                services.RemoveAll(typeof(DbContextOptions<TiendaContext>));
                services.RemoveAll(typeof(TiendaContext));
                services.AddScoped<TiendaContext>(sp =>
                {
                    var session = sp.GetRequiredService<IUserSessionService>();
                    var options = new DbContextOptionsBuilder<TiendaContext>()
                        .UseSqlite(Conexion)
                        .Options;
                    return new E2ETiendaContext(options, session);
                });

                // Sin hosted services: el servidor de Hangfire corre en producción (storage SQL);
                // acá solo interesa el encolado de emails, que usa JobStorage.Current in-memory.
                services.RemoveAll<IHostedService>();
                services.AddHangfire(config => config.UseInMemoryStorage());

                // Los integrados con I/O externo van por fakes: nadie sale del proceso.
                // El tipo explícito es obligatorio: AddSingleton(Pagos) solo registraría
                // la clase concreta y el controller resolvería IPaymentService en null.
                services.RemoveAll<IPaymentService>();
                services.AddSingleton<IPaymentService>(Pagos);
                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService>(Emails);
                services.RemoveAll<IImagenService>();
                services.AddSingleton<IImagenService>(Imagenes);
            });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            // Build #1: host de utilería con TestServer — la base de WebApplicationFactory
            // exige castear ese servidor (StartServer) aunque no lo usemos. No atiende requests.
            var hostUtileria = builder.Build();

            // Build #2: el mismo builder con Kestrel en un puerto libre de loopback;
            // este host es el que SIRVE requests al browser y el que expone la DI
            // completa de la app. La config extra del builder se acumula: ambos
            // builds registran los mismos services (los fakes son instancias de la
            // factory, compartidos por ambos contenedores).
            builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel(k =>
            {
                k.Listen(IPAddress.Loopback, 0);
            }));
            var hostReal = builder.Build();
            hostReal.StartAsync().GetAwaiter().GetResult();
            _hostReal = hostReal;


            BaseUrl = hostReal.Services
                .GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!
                .Addresses
                .First();
            // La dirección dice http://localhost:... o http://127.0.0.1:...: la normalizo
            // a 127.0.0.1 para que cookies y redirects no duden del host.
            BaseUrl = new UriBuilder(BaseUrl) { Host = "127.0.0.1" }.Uri.ToString().TrimEnd('/');

            return hostUtileria;
        }

        protected override void Dispose(bool disposing)
        {
            // El host real corre al lado del que WAF pública en base.Dispose:
            // hay que pararlo primero (el browser ya no lo usa al morir la suite).
            _hostReal?.StopAsync().GetAwaiter().GetResult();
            _hostReal?.Dispose();
            _hostReal = null;

            base.Dispose(disposing);
            if (_jobStoragePrevio != null)
            {
                JobStorage.Current = _jobStoragePrevio;
            }
        }

        public override async ValueTask DisposeAsync()
        {
            if (_hostReal != null)
            {
                await _hostReal.StopAsync();
                _hostReal.Dispose();
                _hostReal = null;
            }

            await base.DisposeAsync();
            if (_jobStoragePrevio != null)
            {
                JobStorage.Current = _jobStoragePrevio;
            }
        }
    }
}

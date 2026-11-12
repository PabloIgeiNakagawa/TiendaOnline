using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.RateLimiting;

namespace TiendaOnline.Extensions
{
    public static class StartupRateLimitingExtensions
    {
        public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services, IHostEnvironment environment)
        {
            // En los hosts de test (WebApplicationFactory) todos los requests salen de
            // la misma IP y las suites disparan varios POSTs seguidos a los mismos
            // endpoints: es simulación, no fuerza bruta real. El margen de test sube
            // los techos para que el límite de producción no convierta POSTs legítimos
            // de la suite en 429 casuales; en Development/Production quedan intactos.
            var margenDeTest = environment.IsEnvironment("Testing");
            int Techo(int techoReal) => margenDeTest ? techoReal * 100 : techoReal;

            services.AddRateLimiter(options =>
            {
                // Login: 10 intentos por minuto por IP
                options.AddFixedWindowLimiter("login", opt =>
                {
                    opt.PermitLimit = Techo(10);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Registro: 5 por minuto por IP (evitar spam)
                options.AddFixedWindowLimiter("registro", opt =>
                {
                    opt.PermitLimit = Techo(5);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Checkout: 5 por minuto por IP (evitar pedidos falsos)
                options.AddFixedWindowLimiter("checkout", opt =>
                {
                    opt.PermitLimit = Techo(5);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Carrito: 30 por minuto por IP (uso normal del sitio)
                options.AddFixedWindowLimiter("carrito", opt =>
                {
                    opt.PermitLimit = Techo(30);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Respuesta personalizada cuando se excede el límite
                options.RejectionStatusCode = 429;
                options.OnRejected = (context, token) =>
                {
                    var httpContext = context.HttpContext;
                    // Cada rechazo queda registrado: es la señal visible de fuerza bruta
                    // contra login/registro, hoy solo haría un redirect que nadie ve.
                    var logger = httpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("TiendaOnline.RateLimit");
                    logger.LogWarning("Rate limit excedido: {Path} desde {IP}",
                        httpContext.Request.Path,
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");

                    httpContext.Response.Redirect("/Home/Error429");
                    return new ValueTask();
                };

                // Comprobante PDF: 10 por minuto por IP (evitar abuso de generación de PDFs)
                options.AddFixedWindowLimiter("comprobante", opt =>
                {
                    opt.PermitLimit = Techo(10);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Subida de comprobante de transferencia: 5 por minuto por IP (la subida dispara email al admin)
                options.AddFixedWindowLimiter("subir-comprobante", opt =>
                {
                    opt.PermitLimit = Techo(5);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Usar IP como clave de rate limiting
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = Techo(100),
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            return services;
        }
    }
}

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
                options.AddFixedWindowLimiter("login", opt =>
                {
                    opt.PermitLimit = Techo(10);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("registro", opt =>
                {
                    opt.PermitLimit = Techo(5);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("checkout", opt =>
                {
                    opt.PermitLimit = Techo(5);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("carrito", opt =>
                {
                    opt.PermitLimit = Techo(30);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

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

                options.AddFixedWindowLimiter("comprobante", opt =>
                {
                    opt.PermitLimit = Techo(10);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("subir-comprobante", opt =>
                {
                    opt.PermitLimit = Techo(5);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                options.AddFixedWindowLimiter("olvide-password", opt =>
                {
                    opt.PermitLimit = Techo(5);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Cambio de contraseña desde el perfil: 5 por minuto por IP (fuerza bruta
                // contra la contraseña actual: el form verifica el hash viejo en cada envío)
                options.AddFixedWindowLimiter("cambiar-password", opt =>
                {
                    opt.PermitLimit = Techo(5);
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

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

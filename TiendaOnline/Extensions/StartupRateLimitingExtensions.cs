using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.RateLimiting;

namespace TiendaOnline.Extensions
{
    public static class StartupRateLimitingExtensions
    {
        public static IServiceCollection AddRateLimitingConfig(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                // Login: 10 intentos por minuto por IP
                options.AddFixedWindowLimiter("login", opt =>
                {
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Registro: 5 por minuto por IP (evitar spam)
                options.AddFixedWindowLimiter("registro", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Checkout: 5 por minuto por IP (evitar pedidos falsos)
                options.AddFixedWindowLimiter("checkout", opt =>
                {
                    opt.PermitLimit = 5;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Carrito: 30 por minuto por IP (uso normal del sitio)
                options.AddFixedWindowLimiter("carrito", opt =>
                {
                    opt.PermitLimit = 30;
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
                    opt.PermitLimit = 10;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 0;
                });

                // Usar IP como clave de rate limiting
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            return services;
        }
    }
}

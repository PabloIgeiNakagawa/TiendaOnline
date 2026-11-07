using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.DependencyInjection;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Filters
{
    /// <summary>
    /// Traduce las DomainException que no atrapó el controller a UX amable: el mensaje de
    /// negocio (curado en el service) va al TempData por la API tipada (SetMensajeError) y el usuario vuelve a
    /// la página de donde vino. Las excepciones que NO son de dominio no se tocan: siguen
    /// al pipeline de errores estándar (y el log de Serilog queda con el detalle).
    /// Cobertura real: los endpoints sin try/catch propio que llaman al dominio
    /// (Enviar/Entregar/Cancelar pedido en el panel, ReintentarPago del cliente, los guards
    /// futuros de nuevos servicios de dominio).
    /// </summary>
    public class DomainExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<DomainExceptionFilter> _logger;

        public DomainExceptionFilter(ILogger<DomainExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            if (context.ExceptionHandled || context.Exception is not DomainException excepcion)
                return;

            _logger.LogWarning(excepcion, "Excepción de dominio no manejada en {Path}: {Motivo}",
                context.HttpContext.Request.Path, excepcion.Message);

            // Misma TempData que usa el controller (la factory la inicializa una sola vez
            // por request, así el redirect la muestra en la página de origen)
            var tempData = context.HttpContext.RequestServices
                .GetRequiredService<ITempDataDictionaryFactory>()
                .GetTempData(context.HttpContext);
            tempData.SetMensajeError(excepcion.Message);

            context.Result = new RedirectResult(ObtenerUrlDeVuelta(context.HttpContext.Request));
            context.ExceptionHandled = true;
        }

        // El Referer es un header manipulable: solo volvemos a la página de origen si es
        // del mismo sitio (defensa contra open redirect). Si no hay referer válido, a Home.
        private static string ObtenerUrlDeVuelta(HttpRequest request)
        {
            var referer = request.Headers.Referer.ToString();
            if (!string.IsNullOrEmpty(referer)
                && Uri.TryCreate(referer, UriKind.Absolute, out var url)
                && string.Equals(url.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase))
            {
                return referer;
            }

            return "/";
        }
    }
}

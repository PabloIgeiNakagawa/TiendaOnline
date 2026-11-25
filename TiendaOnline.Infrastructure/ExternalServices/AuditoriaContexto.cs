using Microsoft.AspNetCore.Http;
using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.Infrastructure.ExternalServices
{
    public class AuditoriaContexto : IAuditoriaContexto
    {
        public const string OrigenWeb = "Web";
        public const string OrigenSistema = "Sistema";

        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditoriaContexto(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? Ip
            => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        public string? UserAgent
        {
            get
            {
                var userAgent = _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
                return string.IsNullOrWhiteSpace(userAgent) ? null : userAgent;
            }
        }

        // Traza del request de ASP.NET: el mismo id que queda en los logs.
        public string? CorrelationId
            => _httpContextAccessor.HttpContext?.TraceIdentifier;

        public string Origen
            => _httpContextAccessor.HttpContext is null ? OrigenSistema : OrigenWeb;
    }
}

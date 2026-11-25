namespace TiendaOnline.Application.Common.Interfaces
{
    /// <summary>
    /// Contexto del request para las filas de auditoría: origen del cambio, IP,
    /// agente del navegador e identificador de traza. Fuera de un request
    /// (jobs de Hangfire) los tres primeros son nulos y el origen es "Sistema".
    /// </summary>
    public interface IAuditoriaContexto
    {
        string? Ip { get; }
        string? UserAgent { get; }
        string? CorrelationId { get; }
        string Origen { get; }
    }
}

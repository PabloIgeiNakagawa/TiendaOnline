namespace TiendaOnline.Application.Envios
{
    /// <summary>
    /// Escrituras de zonas de envío desde el panel de admin. El costo puede ser 0
    /// (envío gratis a esa zona) pero nunca negativo.
    /// </summary>
    public interface IEnvioCommandService
    {
        Task GuardarZonaAsync(int zonaEnvioId, decimal costo, bool activa, CancellationToken ct = default);
    }
}

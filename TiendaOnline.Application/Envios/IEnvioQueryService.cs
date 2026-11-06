namespace TiendaOnline.Application.Envios
{
    /// <summary>
    /// Lecturas de zonas de envío. Único punto donde vive la tarifa de envío a domicilio:
    /// el costo se cotiza contra la tabla de zonas (editable desde el panel de admin).
    /// </summary>
    public interface IEnvioQueryService
    {
        /// <summary>
        /// Costo de envío a domicilio a la provincia. Provincia desconocida (sin fila) 
        /// usa el costo inicial sembrado per defecto. Zona deshabilitada → Permitida = false.
        /// El match es insensible a mayúsculas y espacios.
        /// </summary>
        Task<CotizacionEnvioDto> CotizarEnvioAsync(string? provincia, CancellationToken ct = default);

        /// <summary>Vista del panel de envíos: todas las zonas, ordenadas por provincia.</summary>
        Task<List<ZonaEnvioDto>> ObtenerZonasAsync(CancellationToken ct = default);
    }

    /// <summary>Cotización de un despacho a domicilio.</summary>
    public record CotizacionEnvioDto(decimal Costo, bool Permitida, bool EsCostoDefecto);

    /// <summary>Fila del panel de envíos.</summary>
    public record ZonaEnvioDto(int ZonaEnvioId, string Provincia, decimal Costo, bool Activa);
}

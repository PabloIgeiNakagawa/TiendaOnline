namespace TiendaOnline.Application.Ofertas
{
    /// <summary>Datos del formulario de alta de una oferta. Se reutiliza para editar.</summary>
    public record CrearOfertaDto(
        int ProductoId,
        decimal PrecioOferta,
        DateTime FechaInicio,
        DateTime FechaFin);

    /// <summary>
    /// Escrituras del ABM de ofertas. Las reglas (producto activo, ventana coherente,
    /// precio de oferta menor al de lista) se validan con guards que lanzan OfertaNoValidaException.
    /// </summary>
    public interface IOfertaCommandService
    {
        Task CrearOfertaAsync(CrearOfertaDto dto, CancellationToken ct = default);
        Task EditarOfertaAsync(int ofertaId, CrearOfertaDto dto, CancellationToken ct = default);
        Task AlternarActivaAsync(int ofertaId, CancellationToken ct = default);
    }
}

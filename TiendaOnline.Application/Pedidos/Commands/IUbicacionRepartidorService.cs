namespace TiendaOnline.Application.Pedidos.Commands
{
    /// <summary>
    /// Última posición del repartidor mientras comparte ubicación desde el
    /// teléfono. Una fila por repartidor, sin historial: se pisa en cada latido
    /// y se borra al detener el compartir o al no quedarle entregas en curso.
    /// </summary>
    public interface IUbicacionRepartidorService
    {
        Task RegistrarAsync(double latitud, double longitud, double? precisionMetros, CancellationToken ct = default);

        Task DetenerAsync(CancellationToken ct = default);

        /// <summary>El repartidor logueado tiene una posición publicada.</summary>
        Task<bool> EstaCompartiendoAsync(CancellationToken ct = default);
    }
}

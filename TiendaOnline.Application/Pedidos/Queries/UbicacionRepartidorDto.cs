namespace TiendaOnline.Application.Pedidos.Queries
{
    /// <summary>
    /// Última posición del repartidor para el mapa del pedido. La antigüedad
    /// viaja calculada por el servidor: el navegador del cliente no tiene por
    /// qué compartir reloj con el server (zona horaria, desfasaje).
    /// </summary>
    public record UbicacionRepartidorDto(
        string RepartidorNombre,
        double Latitud,
        double Longitud,
        double? PrecisionMetros,
        int SegundosDesdeActualizacion);
}

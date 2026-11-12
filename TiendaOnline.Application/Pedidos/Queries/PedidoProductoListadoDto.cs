namespace TiendaOnline.Application.Pedidos.Queries
{
    /// <summary>
    /// Producto de un pedido para la lista del usuario: snapshot de display
    /// (nombre, imagen y cantidad pedida) para las miniaturas de Mis Pedidos.
    /// </summary>
    public record PedidoProductoListadoDto(string Nombre, string ImagenUrl, int Cantidad);
}

namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>
    /// Guard de entrada de un pedido nuevo: datos faltantes o inválidos antes
    /// de armarlo (carrito vacío, sin ítems). El Message es curado para UI.
    /// </summary>
    public class PedidoNoValidoException : DomainException
    {
        public PedidoNoValidoException(string message) : base(message) { }
    }
}

namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>El stock no alcanza para cubrir la operación (reserva de pedido).</summary>
    public class StockInsuficienteException : DomainException
    {
        public StockInsuficienteException(string message) : base(message) { }
    }
}

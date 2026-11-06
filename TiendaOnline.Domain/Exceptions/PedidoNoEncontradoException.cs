namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>El pedido referenciado no existe en la base.</summary>
    public class PedidoNoEncontradoException : DomainException
    {
        public PedidoNoEncontradoException(string message) : base(message) { }
    }
}

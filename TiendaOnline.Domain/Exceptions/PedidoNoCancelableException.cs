namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>El pedido está en un estado que no admite la operación pedida (aprobar, rechazar, enviar, entregar, cancelar).</summary>
    public class PedidoNoCancelableException : DomainException
    {
        public PedidoNoCancelableException(string message) : base(message) { }
    }
}

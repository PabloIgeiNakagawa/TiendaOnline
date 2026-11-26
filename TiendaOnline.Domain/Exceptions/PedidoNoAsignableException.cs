namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>El pedido no admite la asignación de repartidor pedida (estado, ya tomado por otro o el usuario elegido no es un repartidor activo).</summary>
    public class PedidoNoAsignableException : DomainException
    {
        public PedidoNoAsignableException(string message) : base(message) { }
    }
}

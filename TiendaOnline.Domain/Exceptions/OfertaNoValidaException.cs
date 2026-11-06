namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>Las reglas de la oferta no cierran (producto inexistente/inactivo, ventana invertida, precio de oferta mayor o igual al de lista).</summary>
    public class OfertaNoValidaException : DomainException
    {
        public OfertaNoValidaException(string message) : base(message) { }
    }
}

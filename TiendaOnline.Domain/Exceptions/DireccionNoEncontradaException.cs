namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>La dirección referenciada no existe en la base (guard de services de direcciones).</summary>
    public class DireccionNoEncontradaException : DomainException
    {
        public DireccionNoEncontradaException(string message) : base(message) { }
    }
}

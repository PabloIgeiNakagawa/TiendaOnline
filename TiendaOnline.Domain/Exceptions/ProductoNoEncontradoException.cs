namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>El producto referenciado no existe (o está inactivo) en la base. El Message es curado para UI.</summary>
    public class ProductoNoEncontradoException : DomainException
    {
        public ProductoNoEncontradoException(string message) : base(message) { }
    }
}

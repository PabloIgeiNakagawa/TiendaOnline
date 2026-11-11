namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>La imagen pedida no existe, no pertenece al producto de la ruta o excede el tope de la galería.</summary>
    public class ImagenNoValidaException : DomainException
    {
        public ImagenNoValidaException(string message) : base(message) { }
    }
}

namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>
    /// Base de las excepciones de reglas de negocio. Su Message está curado para UI:
    /// el DomainExceptionFilter lo expone al usuario (TempData) y deriva el detalle al log.
    /// Las excepciones que NO son de dominio jamás muestran su Message al usuario.
    /// </summary>
    public class DomainException : Exception
    {
        public DomainException(string message) : base(message) { }
    }
}

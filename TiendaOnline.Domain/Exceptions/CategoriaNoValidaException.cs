namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>
    /// Guard de dominio del ABM de categorías: jerarquía inválida (un padre que
    /// haría que la categoría sea ancestro de sí misma). El Message está curado
    /// para mostrar al admin en el panel de categorías.
    /// </summary>
    public class CategoriaNoValidaException : DomainException
    {
        public CategoriaNoValidaException(string message)
            : base(message)
        {
        }
    }
}

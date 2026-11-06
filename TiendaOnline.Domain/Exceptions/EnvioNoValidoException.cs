namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>
    /// Guard de dominio del ABM de zonas de envío: costo negativo o zona inexistente.
    /// El Message está curado para mostrar al admin en el panel de envíos.
    /// </summary>
    public class EnvioNoValidoException : DomainException
    {
        public EnvioNoValidoException(string message)
            : base(message)
        {
        }
    }
}

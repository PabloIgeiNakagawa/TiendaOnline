namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>La contraseña actual ingresada no verifica contra el hash guardado (cambio de contraseña desde el perfil).</summary>
    public class PasswordActualIncorrectaException : DomainException
    {
        public PasswordActualIncorrectaException(string message) : base(message) { }
    }
}

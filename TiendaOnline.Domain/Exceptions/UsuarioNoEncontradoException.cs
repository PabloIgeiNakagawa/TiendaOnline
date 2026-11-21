namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>El usuario referenciado no existe en la base (guard de services de usuarios).</summary>
    public class UsuarioNoEncontradoException : DomainException
    {
        public UsuarioNoEncontradoException(string message) : base(message) { }
    }
}

namespace TiendaOnline.Exceptions
{
    public class EmailDuplicadoException : Exception
    {
        public EmailDuplicadoException(string email)
            : base($"El email '{email}' ya está registrado.") { }
    }
}
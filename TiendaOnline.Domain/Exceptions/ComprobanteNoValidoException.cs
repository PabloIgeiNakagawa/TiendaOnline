namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>El ciclo del comprobante de transferencia no está en el estado correcto (no transfiere, ya subido, sin comprobante, subida fallida).</summary>
    public class ComprobanteNoValidoException : DomainException
    {
        public ComprobanteNoValidoException(string message) : base(message) { }
    }
}

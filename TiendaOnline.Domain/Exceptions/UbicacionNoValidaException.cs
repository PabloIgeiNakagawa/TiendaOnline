namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>La posición informada no es una coordenada válida (latitud/longitud fuera de rango).</summary>
    public class UbicacionNoValidaException : DomainException
    {
        public UbicacionNoValidaException(string message) : base(message) { }
    }
}

namespace TiendaOnline.Domain.Exceptions
{
    /// <summary>
    /// Guard de dominio de los movimientos de stock manuales: cantidades no
    /// positivas, resultados por debajo del piso de 0 o una devolución que
    /// excede la cantidad comprada en el pedido. El Message está curado para
    /// el panel de administración.
    /// </summary>
    public class MovimientoStockNoValidoException : DomainException
    {
        public MovimientoStockNoValidoException(string message)
            : base(message)
        {
        }
    }
}

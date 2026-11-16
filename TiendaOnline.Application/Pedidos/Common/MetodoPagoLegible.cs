namespace TiendaOnline.Application.Pedidos.Common
{
    /// <summary>
    /// Texto de negocio de cada método de pago para salidas legibles (PDF de
    /// comprobante, dashboard). El enum MetodoPago es la única fuente de verdad;
    /// la tabla MetodoDePago se retiró y sus nombres legibles viven acá.
    /// </summary>
    public static class MetodoPagoLegible
    {
        public static string Nombre(TiendaOnline.Domain.Entities.MetodoPago metodo) => metodo switch
        {
            TiendaOnline.Domain.Entities.MetodoPago.MercadoPago => "Mercado Pago",
            TiendaOnline.Domain.Entities.MetodoPago.Transferencia => "Transferencia Bancaria",
            _ => "Efectivo"
        };
    }
}

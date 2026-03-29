using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Enums
{
    public enum MetodoPagoId
    {
        [Display(Name = "Mercado Pago")]
        MercadoPago = 1,
        Transferencia = 2,
        Efectivo = 3
    }
}

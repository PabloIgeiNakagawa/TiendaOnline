using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    /// <summary>
    /// Métodos de pago del checkout: única fuente de verdad del dominio. La base
    /// guarda el texto del enum en Pedidos (migration MetodoPagoComoTexto); la
    /// tabla MetodoDePago se retiró con esa misma migration.
    /// </summary>
    public enum MetodoPago
    {
        [Display(Name = "Mercado Pago")]
        MercadoPago = 1,

        [Display(Name = "Transferencia bancaria")]
        Transferencia = 2,

        [Display(Name = "Efectivo")]
        Efectivo = 3
    }
}

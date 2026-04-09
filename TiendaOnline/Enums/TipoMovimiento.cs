using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Enums
{
    public enum TipoMovimiento
    {
        [Display(Name = "Entrada")]
        EntradaStock = 1,

        [Display(Name = "Salida")]
        SalidaVenta = 2,

        [Display(Name = "Devolución")]
        Devolucion = 3,

        [Display(Name = "Ajuste")]
        AjusteManual = 4,

        [Display(Name = "Cancelación")]
        CancelacionPedido = 5
    }
}

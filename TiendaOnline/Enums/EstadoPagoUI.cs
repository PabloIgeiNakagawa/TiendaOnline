using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Enums
{
    public enum EstadoPagoUI
    {
        [Display(Name = "Pendiente")]
        Pendiente = 0,

        [Display(Name = "Aprobado")]
        Aprobado = 1,

        [Display(Name = "Rechazado")]
        Rechazado = 2,

        [Display(Name = "Reembolsado")]
        Reembolsado = 3
    }
}

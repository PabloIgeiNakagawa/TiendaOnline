using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Enums
{
    public enum EstadoPedidoUI
    {
        [Display(Name = "Nuevo")]
        Nuevo = 0,

        [Display(Name = "En preparación")]
        EnPreparacion = 1, // Ya se pagó, el admin está armando la caja

        [Display(Name = "Enviado")]
        Enviado = 2,

        [Display(Name = "Entregado")]
        Entregado = 3,

        [Display(Name = "Cancelado")]
        Cancelado = 4
    }
}

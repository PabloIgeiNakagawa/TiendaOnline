using System.ComponentModel.DataAnnotations;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Enums;

namespace TiendaOnline.Features.Pedidos
{
    public class ConfirmacionPedidoViewModel
    {
        public List<ItemCarrito> Items { get; set; }
        public decimal SubTotal { get; set; }
        public decimal CostoEnvio { get; set; }
        public MetodoEntrega MetodoEntrega { get; set; }
        public string NombreUsuario { get; set; }
        public string EmailUsuario { get; set; }
        public string TelefonoUsuario { get; set; }
        public DireccionCheckOut Direccion { get; set; }
    }

    public class DireccionCheckOut
    {
        public bool EsNueva { get; set; }

        [MaxLength(20)]
        public string Etiqueta { get; set; }

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [MaxLength(50, ErrorMessage = "La provincia no puede tener más de 50 caracteres.")]
        public string Provincia { get; set; }

        [Required(ErrorMessage = "La localidad es obligatoria.")]
        [MaxLength(50, ErrorMessage = "La localidad no puede tener más de 50 caracteres.")]
        public string Localidad { get; set; }

        [Required(ErrorMessage = "El código postal es obligatorio.")]
        [MaxLength(10, ErrorMessage = "El código postal no puede tener más de 10 caracteres.")]
        public string CodigoPostal { get; set; }

        [Required(ErrorMessage = "La calle es obligatoria.")]
        [MaxLength(100, ErrorMessage = "La calle no puede tener más de 100 caracteres.")]
        public string Calle { get; set; }

        [Required(ErrorMessage = "El número es obligatorio.")]
        [MaxLength(10, ErrorMessage = "El número no puede tener más de 10 caracteres.")]
        public string Numero { get; set; }

        [MaxLength(10)]
        public string? Piso { get; set; }

        [MaxLength(10)]
        public string? Departamento { get; set; }

        [MaxLength(500, ErrorMessage = "Las observaciones no pueden tener más de 500 caracteres.")]
        public string Observaciones { get; set; }
    }
}

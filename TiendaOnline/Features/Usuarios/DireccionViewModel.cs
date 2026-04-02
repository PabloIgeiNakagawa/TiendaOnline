using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Usuarios
{
    public class DireccionViewModel
    {
        public int DireccionId { get; set; }

        [Required(ErrorMessage = "La etiqueta es obligatoria")]
        [MaxLength(20, ErrorMessage = "La etiqueta no puede tener más de 20 caracteres")]
        [Display(Name = "Etiqueta")]
        public string Etiqueta { get; set; }

        [Required(ErrorMessage = "La calle es obligatoria")]
        [MaxLength(100, ErrorMessage = "La calle no puede tener más de 100 caracteres")]
        [Display(Name = "Calle")]
        public string Calle { get; set; }

        [Required(ErrorMessage = "El número es obligatorio")]
        [MaxLength(10, ErrorMessage = "El número no puede tener más de 10 caracteres")]
        [Display(Name = "Número")]
        public string Numero { get; set; }

        [MaxLength(20, ErrorMessage = "El piso no puede tener más de 20 caracteres")]
        [Display(Name = "Piso")]
        public string? Piso { get; set; }

        [MaxLength(20, ErrorMessage = "El departamento no puede tener más de 20 caracteres")]
        [Display(Name = "Departamento")]
        public string? Departamento { get; set; }

        [Required(ErrorMessage = "La localidad es obligatoria")]
        [MaxLength(100, ErrorMessage = "La localidad no puede tener más de 100 caracteres")]
        [Display(Name = "Localidad")]
        public string Localidad { get; set; }

        [Required(ErrorMessage = "La provincia es obligatoria")]
        [MaxLength(100, ErrorMessage = "La provincia no puede tener más de 100 caracteres")]
        [Display(Name = "Provincia")]
        public string Provincia { get; set; }

        [Required(ErrorMessage = "El código postal es obligatorio")]
        [MaxLength(15, ErrorMessage = "El código postal no puede tener más de 15 caracteres")]
        [Display(Name = "Código Postal")]
        public string CodigoPostal { get; set; }

        [MaxLength(250, ErrorMessage = "Las observaciones no pueden tener más de 250 caracteres")]
        [Display(Name = "Observaciones")]
        public string? Observaciones { get; set; }

        [Display(Name = "Dirección principal")]
        public bool EsPrincipal { get; set; }

        public bool Activo { get; set; } = true;

        public int UsuarioId { get; set; }
    }
}

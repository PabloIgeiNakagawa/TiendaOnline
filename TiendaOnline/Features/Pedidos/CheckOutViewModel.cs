using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Pedidos
{
    public class CheckOutViewModel
    {
        // Resumen del Carrito
        public List<CheckOutItemViewModel> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal CostoEnvio { get; set; }
        public decimal Total => SubTotal + CostoEnvio;

        // Datos del Usuario
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;

        // Lógica de Entrega
        [Required(ErrorMessage = "Seleccioná un método de entrega")]
        public string MetodoEntrega { get; set; } = "RetiroLocal"; // "RetiroLocal" o "EnvioDomicilio"

        // Direcciones
        // Para mostrar un Dropdown con las direcciones que el usuario ya guardó antes
        public List<DireccionGuardadaViewModel> DireccionesGuardadas { get; set; } = new();
        public int? DireccionSeleccionadaId { get; set; } // Si elige una existente

        // Si decide cargar una nueva, usamos tu ViewModel actual
        public CheckOutDatosViewModel NuevaDireccion { get; set; } = new();
    }

    public class DireccionGuardadaViewModel
    {
        public int DireccionId { get; set; }
        public string Etiqueta { get; set; } // Ej: "Casa", "Trabajo"
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string Localidad { get; set; }
        public string Provincia { get; set; }
        public string CodigoPostal { get; set; }
        public string? Observaciones { get; set; }
    }

    public class CheckOutItemViewModel
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Cantidad { get; set; }
        public string? ImagenUrl { get; set; }
    }

    public class CheckOutDatosViewModel
    {
        [Required(ErrorMessage = "La etiqueta es obligatoria (Ej: Mi Casa, Trabajo).")]
        [StringLength(50, ErrorMessage = "La etiqueta no puede superar los 50 caracteres.")]
        [Display(Name = "Etiqueta")]
        public string Etiqueta { get; set; }

        [Required(ErrorMessage = "La provincia es obligatoria.")]
        [StringLength(50)]
        public string Provincia { get; set; }

        [Required(ErrorMessage = "La localidad es obligatoria.")]
        [StringLength(100)]
        public string Localidad { get; set; }

        [Required(ErrorMessage = "El código postal es obligatorio.")]
        [DataType(DataType.PostalCode)]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "El código postal debe tener entre 4 y 10 caracteres.")]
        [Display(Name = "Código Postal")]
        public string CodigoPostal { get; set; }

        [Required(ErrorMessage = "El nombre de la calle es obligatorio.")]
        [StringLength(100)]
        public string Calle { get; set; }

        [Required(ErrorMessage = "El número es obligatorio.")]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "El número debe contener solo dígitos.")]
        [StringLength(10)]
        public string Numero { get; set; }

        [StringLength(10)]
        [Display(Name = "Piso (Opcional)")]
        public string? Piso { get; set; }

        [StringLength(10)]
        [Display(Name = "Depto (Opcional)")]
        public string? Departamento { get; set; }

        [StringLength(100)]
        [Display(Name = "Observaciones de entrega (Opcional)")]
        public string? Observaciones { get; set; }
    }
}

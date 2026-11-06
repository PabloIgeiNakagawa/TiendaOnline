using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Ofertas
{
    /// <summary>Formulario de alta/edición de una oferta. OfertaId = 0 significa alta.</summary>
    public class OfertaFormularioViewModel
    {
        public int OfertaId { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un producto")]
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "El precio de oferta es obligatorio")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de oferta debe ser mayor a 0")]
        [Display(Name = "Precio de oferta")]
        public decimal PrecioOferta { get; set; }

        [Required(ErrorMessage = "La fecha de inicio es obligatoria")]
        [Display(Name = "Vigente desde")]
        public DateTime FechaInicio { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "La fecha de fin es obligatoria")]
        [Display(Name = "Vigente hasta")]
        public DateTime FechaFin { get; set; } = DateTime.Now.AddDays(7);

        /// <summary>Solo edición: contexto de la promo (el producto no se reasigna).</summary>
        public string NombreProducto { get; set; } = string.Empty;
        public decimal PrecioLista { get; set; }

        /// <summary>Solo alta: productos activos para el select.</summary>
        public IEnumerable<SelectListItem>? Productos { get; set; }
    }
}

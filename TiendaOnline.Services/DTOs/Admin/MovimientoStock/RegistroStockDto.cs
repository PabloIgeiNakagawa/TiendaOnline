using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Services.DTOs.Admin.MovimientoStock
{
    public class RegistroStockDto
    {
        [Required(ErrorMessage = "El producto es obligatorio")]
        public int ProductoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
        public int Cantidad { get; set; }

        [MaxLength(250, ErrorMessage = "Las observaciones no pueden superar los 250 caracteres")]
        public string? Observaciones { get; set; }
    }
}

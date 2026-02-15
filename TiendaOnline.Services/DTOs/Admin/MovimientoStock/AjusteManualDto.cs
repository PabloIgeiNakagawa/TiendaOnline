using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Services.DTOs.Admin.MovimientoStock
{
    public class AjusteManualDto
    {
        [Required]
        public int ProductoId { get; set; }

        // Aca permitimos negativos porque puede ser una pérdida
        [Required]
        [Range(int.MinValue, int.MaxValue)]
        public int Cantidad { get; set; }

        [Required]
        [MinLength(10, ErrorMessage = "Debes explicar el motivo del ajuste (mínimo 10 caracteres)")]
        public string Observaciones { get; set; }
    }
}

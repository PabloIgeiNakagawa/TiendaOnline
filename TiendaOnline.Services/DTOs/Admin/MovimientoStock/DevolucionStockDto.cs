using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Services.DTOs.Admin.MovimientoStock
{
    public class DevolucionStockDto
    {
        [Required(ErrorMessage = "El producto es obligatorio")]
        public int ProductoId { get; set; }

        [Required(ErrorMessage = "El pedido original es obligatorio para procesar la devolución")]
        public int PedidoId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad a devolver debe ser al menos 1")]
        public int Cantidad { get; set; }

        [Required(ErrorMessage = "Debes indicar el motivo de la devolución")]
        [MinLength(5, ErrorMessage = "El motivo es demasiado corto")]
        public string Observaciones { get; set; }
    }
}

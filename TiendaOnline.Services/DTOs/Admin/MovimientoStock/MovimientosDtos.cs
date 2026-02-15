namespace TiendaOnline.Services.DTOs.Admin.MovimientoStock
{
    public class MovimientosDto
    {
        public int MovimientoId { get; set; }
        public string ProductoNombre { get; set; }
        public string ImagenUrl { get; set; }
        public int Cantidad { get; set; }
        public string Tipo { get; set; } // Entrada, Salida, Ajuste, etc
        public DateTime Fecha { get; set; }
        public int? PedidoId { get; set; }
        public string Observaciones { get; set; }
    }

    public class TipoMovimientoDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
    }

    public class MovimientoFiltrosDto
    {
        public string? Busqueda { get; set; }
        public int? TipoMovimientoId { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int Pagina { get; set; } = 1;
        public int RegistrosPorPagina { get; set; } = 20;
    }
}

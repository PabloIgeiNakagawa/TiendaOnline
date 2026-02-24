namespace TiendaOnline.Application.Productos.Queries
{
    public class ObtenerProductosAdminRequest
    {
        public string? Busqueda { get; init; }
        public int? CategoriaId { get; init; }

        public string? Estado { get; init; }     
        public string? Stock { get; init; }

        public int Pagina { get; init; } = 1;
        public int TamanoPagina { get; init; } = 10;
    }
}

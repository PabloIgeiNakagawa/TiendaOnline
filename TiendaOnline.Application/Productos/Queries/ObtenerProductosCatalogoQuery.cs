namespace TiendaOnline.Application.Productos.Queries
{
    public class ObtenerProductosCatalogoQuery
    {
        public string? Busqueda { get; init; }
        public int? CategoriaId { get; init; }

        public decimal? PrecioMin { get; init; }
        public decimal? PrecioMax { get; init; }

        public string Orden { get; init; }

        public int Pagina { get; init; } = 1;
        public int Cantidad { get; init; } = 10;
    }
}

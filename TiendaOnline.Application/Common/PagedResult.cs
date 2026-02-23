namespace TiendaOnline.Application.Common
{
    public class PagedResult<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalRegistros { get; set; }
        public int PaginaActual { get; set; }
        public int RegistrosPorPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling(TotalRegistros / (double)RegistrosPorPagina);
        public int DesdeRegistro => TotalRegistros == 0 ? 0 : (PaginaActual - 1) * RegistrosPorPagina + 1;
        public int HastaRegistro => Math.Min(PaginaActual * RegistrosPorPagina, TotalRegistros);

        // Constructor para facilitar la creación en el Service
        public PagedResult(IEnumerable<T> items, int total, int pagina, int cantidad)
        {
            Items = items;
            TotalRegistros = total;
            PaginaActual = pagina;
            RegistrosPorPagina = cantidad;
        }
    }
}

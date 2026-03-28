namespace TiendaOnline.Features.Shared.ViewModels
{
    public class PaginacionViewModel
    {
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }

        // Opcional: Por si la acción no siempre es "Listado" o quieres rutear a otro lado
        public string Accion { get; set; } = "Listado";

        // ¡La magia para que sea reutilizable! Aquí pasaremos los filtros (búsqueda, nivel, etc.)
        public Dictionary<string, string> RutasAdicionales { get; set; } = new Dictionary<string, string>();
    }
}

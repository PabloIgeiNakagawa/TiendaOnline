namespace TiendaOnline.Features.Categorias
{
    public class FiltrosListadoViewModel
    {
        public int Pagina { get; set; } = 1;

        public int TamanoPagina { get; set; } = 10;

        public string? Busqueda { get; set; } = null;

        public string? Nivel { get; set; } = null;
    }
}

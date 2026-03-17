namespace TiendaOnline.Features.Usuarios.Admin
{
    public class FiltrosListadoViewModel
    {
        public int Pagina { get; set; } = 1;
        public int TamanoPagina { get; set; } = 10;
        public string? Busqueda { get; set; } = null;
        public string? Rol { get; set; } = null;
        public string? Estado { get; set; } = null;
    }
}

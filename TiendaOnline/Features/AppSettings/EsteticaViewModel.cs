namespace TiendaOnline.Features.AppSettings
{
    public class EsteticaViewModel
    {
        public string NombreDelSitio { get; set; } = string.Empty;

        // URLs actuales (para mostrarlas en la vista)
        public string? LogoUrl { get; set; }
        public string? FavIconUrl { get; set; }

        // Archivos nuevos (si el usuario decide cambiarlos)
        public IFormFile? LogoFile { get; set; }
        public IFormFile? FavIconFile { get; set; }

        public string ColorPrimary { get; set; } = "#000000";

        public string FuenteTitulo { get; set; } = "Arial";
        public string FuenteBody { get; set; } = "Arial";
    }
}

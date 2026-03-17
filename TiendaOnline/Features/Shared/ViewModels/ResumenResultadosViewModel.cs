namespace TiendaOnline.Features.Shared.ViewModels
{
    public class ResumenResultadosViewModel
    {
        public int TotalRegistros { get; set; }
        public int Desde { get; set; }
        public int Hasta { get; set; }
        public string NombreEntidad { get; set; }
        public List<FiltroPill> Filtros { get; set; } = new();
        public string ActionName { get; set; }
    }

    public class FiltroPill
    {
        public string Icono { get; set; } // Ej: "bi-search"
        public string Etiqueta { get; set; } // Ej: "Búsqueda"
        public string Valor { get; set; } // Ej: "Zapatos"
        public string Propiedad { get; set; } // Para el botón de eliminar (X)
    }
}

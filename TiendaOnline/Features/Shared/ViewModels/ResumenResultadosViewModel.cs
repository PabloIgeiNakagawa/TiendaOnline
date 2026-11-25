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
        public FiltroPill(string icono, string etiqueta, string? valor, string propiedad, params string[] propiedadesAdicionales)
        {
            Icono = icono;
            Etiqueta = etiqueta;
            Valor = valor;
            Propiedad = propiedad;
            PropiedadesAdicionales = propiedadesAdicionales.ToList();
        }

        public string Icono { get; set; } // Ej: "bi-search"
        public string Etiqueta { get; set; } // Ej: "Búsqueda"
        public string? Valor { get; set; } // Ej: "Zapatos"; null/vacío = filtro inactivo
        public string Propiedad { get; set; } // Para el botón de eliminar (X)
        // Parámetros extra que limpia la misma X (la píldora de precio saca precioMin y precioMax juntos)
        public List<string> PropiedadesAdicionales { get; set; } = new();
    }
}

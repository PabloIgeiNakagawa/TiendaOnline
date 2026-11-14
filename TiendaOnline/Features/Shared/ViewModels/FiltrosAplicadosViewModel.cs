namespace TiendaOnline.Features.Shared.ViewModels
{
    /// <summary>
    /// Pills de filtros activos con su X para quitar (parcial _FiltrosAplicados).
    /// Cada X reconstruye la query actual sin SU parámetro y vuelve a la página 1.
    /// </summary>
    public class FiltrosAplicadosViewModel
    {
        public List<FiltroPill> Filtros { get; set; } = new();
        public string ActionName { get; set; } = string.Empty;
    }
}

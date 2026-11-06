using TiendaOnline.Application.Ofertas;

namespace TiendaOnline.Features.Ofertas
{
    /// <summary>Listado del ABM de ofertas con contadores por estado.</summary>
    public class OfertaListadoViewModel
    {
        public List<OfertaDto> Ofertas { get; set; } = new();

        public int TotalVigentes => Ofertas.Count(o => o.Estado == "Vigente");
        public int TotalFuturas => Ofertas.Count(o => o.Estado == "Futura");
        public int TotalExpiradas => Ofertas.Count(o => o.Estado == "Expirada");
        public int TotalInactivas => Ofertas.Count(o => o.Estado == "Inactiva");
    }
}

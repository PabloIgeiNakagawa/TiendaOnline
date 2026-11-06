using TiendaOnline.Application.Envios;
using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Features.Envios.Admin
{
    /// <summary>Fila editable del panel de envíos: costo por provincia y toggle de zonas.</summary>
    public class ZonaEnvioFormularioViewModel
    {
        public int ZonaEnvioId { get; set; }

        public string Provincia { get; set; } = string.Empty;

        public bool Activa { get; set; }

        [Range(0, 1_000_000, ErrorMessage = "El costo no puede ser negativo.")]
        [Display(Name = "Costo de envío")]
        public decimal Costo { get; set; }
    }

    /// <summary>Listado del panel de envíos: una tarifa por provincia.</summary>
    public class EnvioListadoViewModel
    {
        public List<ZonaEnvioFormularioViewModel> Zonas { get; set; } = new();
    }
}

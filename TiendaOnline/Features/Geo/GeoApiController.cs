using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.Geo;

namespace TiendaOnline.Features.Geo
{
    [Route("api/[controller]")]
    [ApiController]
    public class GeoApiController : ControllerBase
    {
        private readonly IGeoService _geoService;

        public GeoApiController(IGeoService geoService)
        {
            _geoService = geoService;
        }

        [HttpGet("provincias")]
        public async Task<IActionResult> GetProvincias()
        {
            var data = await _geoService.GetProvinciasAsync();
            return Ok(data);
        }

        [HttpGet("localidades")]
        public async Task<IActionResult> GetLocalidades([FromQuery] string provincia)
        {
            var data = await _geoService.GetLocalidadesAsync(provincia);
            return Ok(data);
        }
    }
}

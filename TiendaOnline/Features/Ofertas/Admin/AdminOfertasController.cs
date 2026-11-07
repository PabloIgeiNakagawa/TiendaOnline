using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Ofertas.Admin
{
    [Route("admin/ofertas")]
    [Authorize(Roles = "Administrador")]
    public class AdminOfertasController : Controller
    {
        private readonly IOfertaCommandService _ofertaCommandService;
        private readonly IOfertaQueryService _ofertaQueryService;
        private readonly ILogger<AdminOfertasController> _logger;

        public AdminOfertasController(IOfertaCommandService ofertaCommandService, IOfertaQueryService ofertaQueryService, ILogger<AdminOfertasController> logger)
        {
            _ofertaCommandService = ofertaCommandService;
            _ofertaQueryService = ofertaQueryService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado()
        {
            var viewModel = new OfertaListadoViewModel
            {
                Ofertas = await _ofertaQueryService.ObtenerTodasAsync(HttpContext.RequestAborted)
            };

            return View(viewModel);
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Crear()
        {
            var model = new OfertaFormularioViewModel
            {
                Productos = await ObtenerListaProductosAsync()
            };

            return View("Formulario", model);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(OfertaFormularioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.Productos = await ObtenerListaProductosAsync();
                return View("Formulario", model);
            }

            await _ofertaCommandService.CrearOfertaAsync(new CrearOfertaDto(
                model.ProductoId, model.PrecioOferta, model.FechaInicio, model.FechaFin), HttpContext.RequestAborted);

            _logger.LogInformation("Oferta creada: producto {ProductoId} a {PrecioOferta} ({Inicio} - {Fin}) por {Admin}",
                model.ProductoId, model.PrecioOferta, model.FechaInicio, model.FechaFin, User.FindFirstValue(ClaimTypes.Email));

            TempData.SetMensajeExito("Oferta creada correctamente.");
            return RedirectToAction(nameof(Listado));
        }

        [HttpGet("[action]/{id}")]
        public async Task<IActionResult> Editar(int id)
        {
            var ofertas = await _ofertaQueryService.ObtenerTodasAsync(HttpContext.RequestAborted);
            var oferta = ofertas.FirstOrDefault(o => o.OfertaId == id);
            if (oferta is null)
                return NotFound();

            var model = new OfertaFormularioViewModel
            {
                OfertaId = oferta.OfertaId,
                ProductoId = oferta.ProductoId,
                NombreProducto = oferta.NombreProducto,
                PrecioLista = oferta.PrecioLista,
                PrecioOferta = oferta.PrecioOferta,
                FechaInicio = oferta.FechaInicio,
                FechaFin = oferta.FechaFin
            };

            return View("Formulario", model);
        }

        [HttpPost("[action]/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, OfertaFormularioViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Re-render con el contexto real de la promo (el POST no trae NombreProducto ni PrecioLista).
                var ofertas = await _ofertaQueryService.ObtenerTodasAsync(HttpContext.RequestAborted);
                var oferta = ofertas.FirstOrDefault(o => o.OfertaId == id);
                model.NombreProducto = oferta?.NombreProducto ?? model.NombreProducto;
                model.PrecioLista = oferta?.PrecioLista ?? model.PrecioLista;
                return View("Formulario", model);
            }

            await _ofertaCommandService.EditarOfertaAsync(id, new CrearOfertaDto(
                model.ProductoId, model.PrecioOferta, model.FechaInicio, model.FechaFin), HttpContext.RequestAborted);

            _logger.LogInformation("Oferta {OfertaId} editada por {Admin}", id, User.FindFirstValue(ClaimTypes.Email));

            TempData.SetMensajeExito("Oferta actualizada correctamente.");
            return RedirectToAction(nameof(Listado));
        }

        [HttpPost("[action]/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlternarActiva(int id)
        {
            await _ofertaCommandService.AlternarActivaAsync(id, HttpContext.RequestAborted);
            _logger.LogInformation("Oferta {OfertaId} alternada por {Admin}", id, User.FindFirstValue(ClaimTypes.Email));
            TempData.SetMensajeExito("Estado de la oferta actualizado.");
            return RedirectToAction(nameof(Listado));
        }

        private async Task<IEnumerable<SelectListItem>> ObtenerListaProductosAsync()
        {
            var productos = await _ofertaQueryService.ObtenerProductosParaOfertaAsync(HttpContext.RequestAborted);
            return productos.Select(p => new SelectListItem
            {
                Value = p.ProductoId.ToString(),
                Text = $"{p.Nombre} — lista: {p.PrecioLista:C0}"
            });
        }
    }
}

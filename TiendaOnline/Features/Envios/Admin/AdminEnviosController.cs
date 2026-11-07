using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.Envios;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Envios.Admin
{
    [Route("admin/envios")]
    [Authorize(Roles = "Administrador")]
    public class AdminEnviosController : Controller
    {
        private readonly IEnvioQueryService _envioQueryService;
        private readonly IEnvioCommandService _envioCommandService;
        private readonly ILogger<AdminEnviosController> _logger;

        public AdminEnviosController(IEnvioQueryService envioQueryService, IEnvioCommandService envioCommandService, ILogger<AdminEnviosController> logger)
        {
            _envioQueryService = envioQueryService;
            _envioCommandService = envioCommandService;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> Listado()
        {
            var zonas = await _envioQueryService.ObtenerZonasAsync(HttpContext.RequestAborted);

            var viewModel = new EnvioListadoViewModel
            {
                Zonas = zonas.Select(z => new ZonaEnvioFormularioViewModel
                {
                    ZonaEnvioId = z.ZonaEnvioId,
                    Provincia = z.Provincia,
                    Costo = z.Costo,
                    Activa = z.Activa
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpPost("[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(ZonaEnvioFormularioViewModel model)
        {
            // El listado siempre re-rende desde la DB: el POST solo trae esta fila.
            async Task<EnvioListadoViewModel> ArmarListado()
            {
                var zonas = await _envioQueryService.ObtenerZonasAsync(HttpContext.RequestAborted);
                return new EnvioListadoViewModel
                {
                    Zonas = zonas.Select(z => new ZonaEnvioFormularioViewModel
                    {
                        ZonaEnvioId = z.ZonaEnvioId,
                        Provincia = z.Provincia,
                        Costo = z.Costo,
                        Activa = z.Activa
                    }).ToList()
                };
            }

            if (!ModelState.IsValid)
            {
                TempData.SetMensajeError("Revisá el costo: tiene que ser un número entre 0 y 1.000.000.");
                return View("Listado", await ArmarListado());
            }

            try
            {
                await _envioCommandService.GuardarZonaAsync(model.ZonaEnvioId, model.Costo, model.Activa, HttpContext.RequestAborted);

                _logger.LogInformation("Zona de envío {ZonaEnvioId} guardada por {Admin}",
                    model.ZonaEnvioId, User.FindFirstValue(ClaimTypes.Email));

                TempData.SetMensajeExito($"Costo de envío de {model.Provincia} actualizado.");
            }
            catch (DomainException ex)
            {
                // Guard de dominio (zona inexistente, costo negativo): mensaje curado del service.
                _logger.LogWarning(ex, "No se pudo guardar la zona de envío {ZonaEnvioId}: {Motivo}", model.ZonaEnvioId, ex.Message);
                TempData.SetMensajeError(ex.Message);
                return View("Listado", await ArmarListado());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fallo al guardar la zona de envío {ZonaEnvioId}", model.ZonaEnvioId);
                TempData.SetMensajeError("No se pudo guardar la zona de envío. Intentá de nuevo en unos minutos.");
                return View("Listado", await ArmarListado());
            }

            return RedirectToAction(nameof(Listado));
        }
    }
}

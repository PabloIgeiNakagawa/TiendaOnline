using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Features.Shared;
using TiendaOnline.Features.Shared.Controllers;

namespace TiendaOnline.Features.AppSettings
{
    [Route("admin/app-settings")]
    [Authorize(Roles = "Administrador")]
    public class AppSettingsController : AdminControllerBase
    {
        private readonly IAppSettingsService _appSettingsService;
        private readonly IImagenService _imagenService;
        private readonly ILogger<AppSettingsController> _logger;

        public AppSettingsController(IAppSettingsService appSettingsService, IImagenService imagenService, ILogger<AppSettingsController> logger)
        {
            _appSettingsService = appSettingsService;
            _imagenService = imagenService;
            _logger = logger;
        }

        [HttpGet("{groupName}")]
        public async Task<IActionResult> Index(string groupName)
        {
            var settingsDto = await _appSettingsService.GetGroupSettingsAsync(groupName, HttpContext.RequestAborted);

            var model = new IndexViewModel
            {
                GroupName = groupName,
                Settings = settingsDto.Select(s => new AppSettingViewModel
                {
                    Key = s.Key,
                    Value = s.Value,
                    Group = s.Group,
                    IsSensitive = s.IsSensitive,
                    Description = s.Description,
                    Type = s.Type
                }).ToList()
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarConfiguracion(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }
            var dtos = model.Settings.Select(s => new SettingDto
            {
                Key = s.Key,
                Value = s.Value,
                Group = s.Group,
            });

            await _appSettingsService.SaveMultipleSettingsAsync(dtos, HttpContext.RequestAborted);
            _logger.LogInformation("Configuración actualizada: grupo {Grupo} ({Cantidad} claves) por {Admin}",
                model.GroupName, model.Settings.Count, User.FindFirstValue(ClaimTypes.NameIdentifier));
            TempData.SetMensajeExito("Cambios guardados correctamente.");
            return RedirectToAction("Index", new { groupName = model.GroupName });
        }

        [HttpGet("estetica")]
        public IActionResult Estetica()
        {
            var model = new EsteticaViewModel
            {
                NombreDelSitio = _appSettingsService.GetValue(AppSettingsKeys.NombreDelSitio),
                LogoUrl = _appSettingsService.GetValue(AppSettingsKeys.LogoUrl),
                FavIconUrl = _appSettingsService.GetValue(AppSettingsKeys.FavIconUrl),
                ColorPrimary = _appSettingsService.GetValue(AppSettingsKeys.ColorPrimary, "#0d6efd"),
                FuenteTitulo = _appSettingsService.GetValue(AppSettingsKeys.FuenteTitulo, "Poppins"),
                FuenteBody = _appSettingsService.GetValue(AppSettingsKeys.FuenteBody, "Roboto")
            };

            return View(model);
        }

        [HttpPost("guardar-estetica")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEstetica(EsteticaViewModel model)
        {
            if (!ModelState.IsValid) return View(nameof(Estetica), model);

            var imagenesFallidas = new List<string>();

            string logoUrl = model.LogoUrl ?? "";
            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                logoUrl = await SubirConservandoLaAnteriorAsync(model.LogoFile, logoUrl, "el logo", imagenesFallidas);
            }

            string favIconUrl = model.FavIconUrl ?? "";
            if (model.FavIconFile != null && model.FavIconFile.Length > 0)
            {
                favIconUrl = await SubirConservandoLaAnteriorAsync(model.FavIconFile, favIconUrl, "el favicon", imagenesFallidas);
            }

            var settingsToUpdate = new List<SettingDto>
            {
                new() { Key = AppSettingsKeys.NombreDelSitio, Value = model.NombreDelSitio, Group = "Estetica" },
                new() { Key = AppSettingsKeys.LogoUrl, Value = logoUrl, Group = "Estetica" },
                new() { Key = AppSettingsKeys.FavIconUrl, Value = favIconUrl, Group = "Estetica" },
                new() { Key = AppSettingsKeys.ColorPrimary, Value = model.ColorPrimary, Group = "Estetica" },
                new() { Key = AppSettingsKeys.FuenteTitulo, Value = model.FuenteTitulo, Group = "Estetica" },
                new() { Key = AppSettingsKeys.FuenteBody, Value = model.FuenteBody, Group = "Estetica" }
            };

            await _appSettingsService.SaveMultipleSettingsAsync(settingsToUpdate, HttpContext.RequestAborted);

            await _appSettingsService.RefreshCache();

            _logger.LogInformation("Estética del sitio actualizada por {Admin}", User.FindFirstValue(ClaimTypes.NameIdentifier));

            if (imagenesFallidas.Count > 0)
            {
                TempData.SetMensajeError(
                    $"No se pudo subir {string.Join(" ni ", imagenesFallidas)}: se mantienen las imágenes actuales. Los demás cambios se guardaron.");
            }
            else
            {
                TempData.SetMensajeExito("Apariencia actualizada correctamente.");
            }

            return RedirectToAction(nameof(Estetica));
        }

        /// <summary>
        /// Sube la imagen y devuelve la URL nueva. Si la subida falla o no devuelve
        /// URL, conserva la anterior y anota el nombre de la imagen para el aviso:
        /// un logo vacío dejaría la tienda sin identidad sin que el admin se entere.
        /// </summary>
        private async Task<string> SubirConservandoLaAnteriorAsync(
            IFormFile archivo, string urlActual, string descripcion, List<string> fallidas)
        {
            try
            {
                using var stream = archivo.OpenReadStream();
                var url = await _imagenService.SubirImagenAsync(
                    stream, archivo.FileName, "Branding", ct: HttpContext.RequestAborted);

                if (!string.IsNullOrWhiteSpace(url))
                    return url;

                _logger.LogWarning("La subida de {Imagen} ({Archivo}) no devolvió URL; se conserva la anterior.",
                    descripcion, archivo.FileName);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Falló la subida de {Imagen} ({Archivo}); se conserva la anterior.",
                    descripcion, archivo.FileName);
            }

            fallidas.Add(descripcion);
            return urlActual;
        }
    }
}

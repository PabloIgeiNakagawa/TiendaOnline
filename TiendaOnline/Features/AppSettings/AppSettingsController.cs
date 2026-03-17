using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.Features.AppSettings
{
    [Route("admin/app-settings")]
    [Authorize(Roles = "Administrador")]
    public class AppSettingsController : Controller
    {
        private readonly IAppSettingsService _appSettingsService;
        private readonly IImagenService _imagenService;

        public AppSettingsController(IAppSettingsService appSettingsService, IImagenService imagenService)
        {
            _appSettingsService = appSettingsService;
            _imagenService = imagenService;
        }

        [HttpGet("{groupName}")]
        public async Task<IActionResult> Index(string groupName)
        {
            var settingsDto = await _appSettingsService.GetGroupSettingsAsync(groupName);

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

            await _appSettingsService.SaveMultipleSettingsAsync(dtos);
            TempData["MensajeExito"] = "Cambios guardados correctamente.";
            return RedirectToAction("Index", new { groupName = model.GroupName });
        }

        [HttpGet("estetica")]
        public IActionResult Estetica()
        {
            var model = new EsteticaViewModel
            {
                NombreDelSitio = _appSettingsService.GetValue("Diseno:NombreDelSitio"),
                LogoUrl = _appSettingsService.GetValue("Diseno:LogoUrl"),
                FavIconUrl = _appSettingsService.GetValue("Diseno:FavIconUrl"),
                ColorPrimary = _appSettingsService.GetValue("Diseno:ColorPrimary", "#0d6efd"),
                FuenteTitulo = _appSettingsService.GetValue("Diseno:FuenteTitulo", "Poppins"),
                FuenteBody = _appSettingsService.GetValue("Diseno:FuenteBody", "Roboto")
            };

            return View(model);
        }

        [HttpPost("guardar-estetica")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarEstetica(EsteticaViewModel model)
        {
            if (!ModelState.IsValid) return View(nameof(Estetica), model);

            string logoUrl = model.LogoUrl ?? "";

            if (model.LogoFile != null && model.LogoFile.Length > 0)
            {
                using (var stream = model.LogoFile.OpenReadStream())
                {
                    logoUrl = await _imagenService.SubirImagenAsync(stream, model.LogoFile.FileName, model.LogoFile.ContentType);
                }
            }

            string favIconUrl = model.FavIconUrl ?? "";

            if (model.FavIconFile != null && model.FavIconFile.Length > 0)
            {
                using (var stream = model.FavIconFile.OpenReadStream())
                {
                    favIconUrl = await _imagenService.SubirImagenAsync(stream, model.FavIconFile.FileName, model.FavIconFile.ContentType);
                }
            }

            var settingsToUpdate = new List<SettingDto>
            {
                new() { Key = "Diseno:NombreDelSitio", Value = model.NombreDelSitio, Group = "Estetica" },
                new() { Key = "Diseno:LogoUrl", Value = logoUrl, Group = "Estetica" },
                new() { Key = "Diseno:FavIconUrl", Value = favIconUrl, Group = "Estetica" },
                new() { Key = "Diseno:ColorPrimary", Value = model.ColorPrimary, Group = "Estetica" },
                new() { Key = "Diseno:FuenteTitulo", Value = model.FuenteTitulo, Group = "Estetica" },
                new() { Key = "Diseno:FuenteBody", Value = model.FuenteBody, Group = "Estetica" }
            };

            await _appSettingsService.SaveMultipleSettingsAsync(settingsToUpdate);

            await _appSettingsService.RefreshCache();

            TempData["MensajeExito"] = "Apariencia actualizada correctamente.";
            return RedirectToAction(nameof(Estetica));
        }
    }
}

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.AppSettings
{
    public class AppSettingsService : IAppSettingsService
    {
        private readonly TiendaContext _context;
        private readonly IDataProtector _protector;
        private readonly IMemoryCache _cache;
        private const string CacheKey = "App_Settings_Cache";

        public AppSettingsService(TiendaContext context, IDataProtectionProvider provider, IMemoryCache cache)
        {
            _context = context;
            _protector = provider.CreateProtector("TiendaEcommerce.Settings.v1");
            _cache = cache;
        }

        public string GetValue(string key, string defaultValue = "")
        {
            var settings = GetAllFromCache();
            return settings.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public async Task<List<SettingDto>> GetGroupSettingsAsync(string groupName)
        {
            var settings = await _context.AppSettings.AsNoTracking().Where(x => x.Group == groupName).ToListAsync();
            return settings.Select(s => new SettingDto
            {
                Key = s.Key,
                Group = s.Group,
                IsSensitive = s.IsSensitive,
                Value = (s.IsSensitive && !string.IsNullOrEmpty(s.Value)) ? _protector.Unprotect(s.Value) : s.Value,
                Description = s.Description,
                Type = s.Type
            }).ToList();
        }

        public async Task SaveMultipleSettingsAsync(IEnumerable<SettingDto> dtos)
        {
            var keys = dtos.Select(d => d.Key).ToList();
            var settingsExistentes = await _context.AppSettings
                .Where(s => keys.Contains(s.Key))
                .ToListAsync();

            foreach (var dto in dtos)
            {
                var setting = settingsExistentes.FirstOrDefault(s => s.Key == dto.Key);

                if (setting != null)
                {
                    if (setting.IsSensitive && !string.IsNullOrEmpty(dto.Value))
                    {
                        setting.Value = _protector.Protect(dto.Value);
                    }
                    else
                    {
                        setting.Value = dto.Value;
                    }
                }
            }

            await _context.SaveChangesAsync();
            await RefreshCache();
        }

        public Task RefreshCache()
        {
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }

        private Dictionary<string, string> GetAllFromCache()
        {
            return _cache.GetOrCreate(CacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);

                var dbSettings = _context.AppSettings.AsNoTracking().ToList();
                var dict = new Dictionary<string, string>();

                foreach (var s in dbSettings)
                {
                    // Guardamos en caché ya desencriptado para que GetValue sea veloz
                    dict[s.Key] = TryUnprotect(s.Value, s.IsSensitive);
                }
                return dict;
            })!;
        }

        private string TryUnprotect(string value, bool isSensitive)
        {
            if (!isSensitive || string.IsNullOrEmpty(value)) return value ?? string.Empty;
            try
            {
                return _protector.Unprotect(value);
            }
            catch
            {
                return "[ERROR: Payload Corrupto]";
            }
        }
    }
}

namespace TiendaOnline.Application.AppSettings
{
    public interface IAppSettingsService
    {
        string GetValue(string key, string defaultValue = "");
        Task<List<SettingDto>> GetGroupSettingsAsync(string groupName, CancellationToken ct = default);
        Task RefreshCache();
        Task SaveMultipleSettingsAsync(IEnumerable<SettingDto> dtos, CancellationToken ct = default);
    }
}

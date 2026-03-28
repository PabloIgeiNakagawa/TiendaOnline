namespace TiendaOnline.Application.AppSettings
{
    public interface IAppSettingsService
    {
        string GetValue(string key, string defaultValue = "");
        Task<List<SettingDto>> GetGroupSettingsAsync(string groupName);
        Task RefreshCache();
        Task SaveMultipleSettingsAsync(IEnumerable<SettingDto> dtos);
    }
}

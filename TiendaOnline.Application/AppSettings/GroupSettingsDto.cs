namespace TiendaOnline.Application.AppSettings
{
    public class GroupSettingsDto
    {
        public string GroupName { get; set; } = null!;
        public List<SettingDto> Settings { get; set; } = new();
    }
}

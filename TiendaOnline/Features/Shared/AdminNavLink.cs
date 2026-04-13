namespace TiendaOnline.Features.Shared
{
    public class AdminNavLink
    {
        public string Controller { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string? GroupName { get; set; }
    }
}

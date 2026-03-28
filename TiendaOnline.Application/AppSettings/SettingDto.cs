namespace TiendaOnline.Application.AppSettings
{
    public class SettingDto
    {
        public string Key { get; set; } = null!;

        public string? Value { get; set; }

        public string? Group { get; set; }

        public bool IsSensitive { get; set; }

        public string Description { get; set; } = null!;

        public string Type { get; set; } = "string";
         public DateTime? LastModified { get; set; }
    }
}

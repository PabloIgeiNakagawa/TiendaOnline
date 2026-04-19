namespace TiendaOnline.Application.Common.Settings
{
    public class ResendSettings
    {
        public string ApiKey { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }

    public class CloudinarySettings
    {
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
    }

    public class MercadoPagoSettings
    {
        public string PublicKey { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public MercadoPagoBackUrls BackUrls { get; set; } = new();
        public string NotificationUrl { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }

    public class MercadoPagoBackUrls
    {
        public string Success { get; set; } = string.Empty;
        public string Failure { get; set; } = string.Empty;
        public string Pending { get; set; } = string.Empty;
    }

    public class GlobalSettings
    {
        public string SiteUrl { get; set; } = string.Empty;
    }
}
namespace TiendaOnline.Features.Pagos
{
    public class MercadoPagoWebhookDto
    {
        public string Action { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public WebhookDataDto Data { get; set; } = new();

        public class WebhookDataDto
        {
            public string Id { get; set; } = string.Empty;
        }
    }
}

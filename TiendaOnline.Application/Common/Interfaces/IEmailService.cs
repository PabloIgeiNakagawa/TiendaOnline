namespace TiendaOnline.Application.Common.Interfaces
{
    public interface IEmailService
    {
        Task EnviarEmailAsync(string destino, string asunto, string cuerpoHtml);
        Task EnviarConfirmacionPedidoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total);
        Task EnviarEmailPagoExitosoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total);
        Task EnviarEmailPagoRechazadoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total);
        Task EnviarEmailPagoReembolsadoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total);
        Task EnviarEmailPedidoVencidoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total);
    }
}

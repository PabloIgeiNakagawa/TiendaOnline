using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.E2E.Host
{
    /// <summary>
    /// Email de mentira: no habla con Resend. Registra lo que le llegó para que un
    /// smoke test quiera asserts sobre notificaciones (los smoke tests v1 no lo usan).
    /// </summary>
    public class E2EEmailFalso : IEmailService
    {
        public List<(string Destino, string Asunto)> Enviados { get; } = new();

        private Task RegistrarAsync(string destino, string asunto)
        {
            Enviados.Add((destino, asunto));
            return Task.CompletedTask;
        }

        public Task EnviarEmailAsync(string destino, string asunto, string cuerpoHtml)
            => RegistrarAsync(destino, asunto);

        public Task EnviarConfirmacionPedidoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
            => RegistrarAsync(destinoEmail, $"Pedido #{pedidoId:D6} confirmado");

        public Task EnviarEmailPagoExitosoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
            => RegistrarAsync(destinoEmail, $"Pago aprobado del pedido #{pedidoId:D6}");

        public Task EnviarEmailPagoRechazadoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
            => RegistrarAsync(destinoEmail, $"Pago rechazado del pedido #{pedidoId:D6}");

        public Task EnviarEmailPagoReembolsadoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
            => RegistrarAsync(destinoEmail, $"Pago reembolsado del pedido #{pedidoId:D6}");

        public Task EnviarEmailPedidoVencidoAsync(string destinoEmail, string nombreUsuario, int pedidoId, decimal total)
            => RegistrarAsync(destinoEmail, $"Pedido #{pedidoId:D6} vencido");

        public Task EnviarEmailComprobanteRecibidoAsync(string destinoEmail, int pedidoId)
            => RegistrarAsync(destinoEmail, $"Comprobante del pedido #{pedidoId:D6} recibido");

        public Task EnviarEmailPedidoEntregadoAsync(string destinoEmail, string nombreUsuario, int pedidoId)
            => RegistrarAsync(destinoEmail, $"Pedido #{pedidoId:D6} entregado");

        public Task EnviarEmailResetPasswordAsync(string destinoEmail, string nombreUsuario, string token)
            => RegistrarAsync(destinoEmail, "Recuperación de contraseña");

        public Task EnviarEmailPasswordCambiadaAsync(string destinoEmail, string nombreUsuario)
            => RegistrarAsync(destinoEmail, "Tu contraseña fue cambiada");
    }
}

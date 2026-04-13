namespace TiendaOnline.Application.Pedidos.Command
{
    public class CrearPedidoDto
    {
        // Datos del pedido
        public int UsuarioId { get; set; }
        public int MetodoDePagoId { get; set; }

        // Datos de envío
        public bool EsEnvioADomicilio { get; set; }
        public string? EnvioCalle { get; set; }
        public string? EnvioNumero { get; set; }
        public string? EnvioPiso { get; set; }
        public string? EnvioDepartamento { get; set; }
        public string? EnvioObservaciones { get; set; }
        public string? EnvioLocalidad { get; set; }
        public string? EnvioProvincia { get; set; }
        public string? EnvioCodigoPostal { get; set; }

        public decimal CostoEnvio { get; set; }

        public List<CrearPedidoDetalleDto> Items { get; set; } = new();
    }

    public class CrearPedidoDetalleDto
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }
}

using TiendaOnline.Application.Carritos;

namespace TiendaOnline.Features.Pedidos
{
    public class ConfirmacionPedidoViewModel
    {
        public List<ItemCarrito> Items { get; set; }
        public decimal SubTotal { get; set; }
        public decimal CostoEnvio { get; set; }
        public string MetodoEntrega { get; set; } // "Retiro" o "Envio"
        public string NombreUsuario { get; set; }
        public string EmailUsuario { get; set; }
        public DireccionCheckOut Direccion { get; set; }
    }

    public class DireccionCheckOut
    {
        public string Etiqueta { get; set; }
        public string Provincia { get; set; }
        public string Localidad { get; set; }
        public string CodigoPostal { get; set; }
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string Observaciones { get; set; }
    }
}

using TiendaOnline.Application.Carritos;
using TiendaOnline.Enums;

namespace TiendaOnline.Features.Pedidos
{
    public class ConfirmacionPedidoViewModel
    {
        public List<ItemCarrito> Items { get; set; }
        public decimal SubTotal { get; set; }
        public decimal CostoEnvio { get; set; }
        public MetodoEntrega MetodoEntrega { get; set; }
        public string NombreUsuario { get; set; }
        public string EmailUsuario { get; set; }
        public string TelefonoUsuario { get; set; }
        public DireccionCheckOut Direccion { get; set; }
    }

    public class DireccionCheckOut
    {
        public bool EsNueva { get; set; }
        public string Etiqueta { get; set; }
        public string Provincia { get; set; }
        public string Localidad { get; set; }
        public string CodigoPostal { get; set; }
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string? Piso { get; set; }
        public string? Departamento { get; set; }
        public string Observaciones { get; set; }
    }
}

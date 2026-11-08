using MercadoPago.Client.Preference;
using TiendaOnline.Application.Payment;
using Xunit;

namespace TiendaOnline.Tests.Services.Payment
{
    /// <summary>
    /// Tests del armado de ítems de la preferencia de MercadoPago: los productos
    /// con el snapshot cobrado más el ítem "Envío" cuando el pedido tiene costo de
    /// envío — la sumatoria del cobro matchea el total que valida ConfirmarPagoAsync.
    /// El método es estático y puro: no necesita SDK ni base de datos.
    /// </summary>
    public class MercadoPagoServiceArmarItemsTests
    {
        private static PedidoPagoDto Dto(decimal costoEnvio, params (string Nombre, int Cantidad, decimal Precio)[] items)
            => new()
            {
                PedidoId = 7,
                EmailUsuario = "cliente@techstore.com",
                CostoEnvio = costoEnvio,
                Items = items.Select(i => new ItemPagoDto { Nombre = i.Nombre, Cantidad = i.Cantidad, PrecioUnitario = i.Precio }).ToList()
            };

        [Fact]
        public void ArmarItemsDePreferencia_RetiroLocal_ListaSoloConLosProductos()
        {
            var dto = Dto(costoEnvio: 0m, ("Teclado", 1, 100m), ("Mouse", 2, 50m));

            var items = MercadoPagoService.ArmarItemsDePreferencia(dto, "TechStore");

            Assert.Equal(2, items.Count);
            Assert.Equal("Teclado", items[0].Title);
            Assert.Equal("Compra en TechStore - Teclado", items[0].Description);
            Assert.Equal(1, items[0].Quantity);
            Assert.Equal(100m, items[0].UnitPrice);
            Assert.All(items, i => Assert.Equal("ARS", i.CurrencyId));
            Assert.Equal(200m, decimal.Round(items.Sum(i => (decimal)i.UnitPrice * i.Quantity!.Value), 2)); // subtotal, sin ítem de envío
        }

        [Fact]
        public void ArmarItemsDePreferencia_EnvioADomicilio_SumaElItemDeEnvio()
        {
            var dto = Dto(costoEnvio: 50m, ("Teclado", 2, 100m));

            var items = MercadoPagoService.ArmarItemsDePreferencia(dto, "TechStore");

            var envio = Assert.Single(items, i => i.Title == "Envío");
            Assert.Equal("Envío a domicilio - Pedido #7", envio.Description);
            Assert.Equal(1, envio.Quantity);
            Assert.Equal(50m, envio.UnitPrice);
            Assert.Equal("ARS", envio.CurrencyId);

            var producto = Assert.Single(items, i => i.Title == "Teclado");
            Assert.Equal(2, producto.Quantity);
            Assert.Equal(100m, producto.UnitPrice);

            Assert.Equal(250m, decimal.Round(items.Sum(i => (decimal)i.UnitPrice * i.Quantity!.Value), 2)); // subtotal 200 + envío 50
        }
    }
}

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Integration
{
    /// <summary>
    /// La galería de imágenes de punta a punta: el visor de la ficha arma slides
    /// con la principal + las extras, y la baja de una extra en el panel es un
    /// POST con antiforgery donde el par (producto, imagen) de la ruta autoriza.
    /// </summary>
    [Collection("Integracion")] // un host por vez: los hosts comparten App_Data/Serilog del proceso
    public class ImagenesProductoIntegrationTests : IClassFixture<TestApp>
    {
        private readonly TestApp _app;

        public ImagenesProductoIntegrationTests(TestApp app)
        {
            _app = app;
        }

        private static int SembrarProductoConGaleria(TiendaContext ctx)
        {
            var producto = TestDatabase.SeedProducto(ctx);
            ctx.ImagenesProducto.Add(new ProductoImagen { ProductoId = producto.ProductoId, Url = "https://img.test/extra-1.png", Orden = 1 });
            ctx.ImagenesProducto.Add(new ProductoImagen { ProductoId = producto.ProductoId, Url = "https://img.test/extra-2.png", Orden = 2 });
            ctx.SaveChanges();
            return producto.ProductoId;
        }

        private static string ExtraerTokenAntiforgery(string html)
        {
            var coincidencia = Regex.Match(
                html,
                "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");

            Assert.True(coincidencia.Success, "El formulario no renderizó el token de antiforgery.");
            return coincidencia.Groups[1].Value;
        }

        [Fact]
        public async Task Detalles_ConGaleria_RenderizaElVisor()
        {
            _app.ResetMocks();
            var productoId = _app.WithDb(SembrarProductoConGaleria);

            var client = _app.CreateAnonymousClient(); // página pública
            var page = await client.GetAsync($"/Productos/Detalles?id={productoId}");

            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync();

            // El visor: principal como slide 0 y las extras como slides siguientes
            Assert.Contains("id=\"visorProducto\"", html);
            Assert.Contains("https://img.test/producto.png", html);
            Assert.Contains("https://img.test/extra-1.png", html);
            Assert.Contains("https://img.test/extra-2.png", html);
            Assert.Contains("data-bs-slide-to=\"2\"", html);
        }

        [Fact]
        public async Task EliminarImagen_ConAntiforgery_BajaLaFila()
        {
            _app.ResetMocks();
            _app.Imagen.Setup(x => x.ExtraerPublicIdDesdeUrl(It.IsAny<string>())).Returns("id-extra");
            var ids = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "imagenes-admin@test.local");
                var productoId = SembrarProductoConGaleria(ctx);
                var filaId = ctx.ImagenesProducto.Single(i => i.ProductoId == productoId && i.Orden == 1).ProductoImagenId;
                return (AdminId: admin.UsuarioId, ProductoId: productoId, FilaId: filaId);
            });

            var client = _app.CreateClientAs(ids.AdminId, new[] { "Administrador" });
            var page = await client.GetAsync($"/admin/productos/EditarProducto/{ids.ProductoId}");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var html = await page.Content.ReadAsStringAsync();
            Assert.Contains($"EliminarImagen/{ids.ProductoId}/{ids.FilaId}", html);
            var token = ExtraerTokenAntiforgery(html);

            var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
            var response = await client.PostAsync(
                $"/admin/productos/EliminarImagen/{ids.ProductoId}/{ids.FilaId}",
                new FormUrlEncodedContent(form));

            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            _app.Imagen.Verify(x => x.BorrarImagenAsync("id-extra", It.IsAny<CancellationToken>()), Times.Once);
            var fila = _app.WithDb(ctx => ctx.ImagenesProducto.AsNoTracking()
                .SingleOrDefault(i => i.ProductoImagenId == ids.FilaId));
            Assert.Null(fila);
        }

        [Fact]
        public async Task EliminarImagen_DeOtroProducto_MensajeAmableYFilaIntacta()
        {
            _app.ResetMocks();
            var ids = _app.WithDb(ctx =>
            {
                var admin = TestDatabase.SeedUsuario(ctx, "imagenes-admin-ajeno@test.local");
                var productoId = SembrarProductoConGaleria(ctx);
                var filaId = ctx.ImagenesProducto.Single(i => i.ProductoId == productoId && i.Orden == 1).ProductoImagenId;
                return (AdminId: admin.UsuarioId, ProductoId: productoId, FilaId: filaId);
            });

            var client = _app.CreateClientAs(ids.AdminId, new[] { "Administrador" });
            var page = await client.GetAsync($"/admin/productos/EditarProducto/{ids.ProductoId}");
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            var token = ExtraerTokenAntiforgery(await page.Content.ReadAsStringAsync());

            // El id de producto de la ruta NO es el dueño de la imagen
            var form = new Dictionary<string, string> { ["__RequestVerificationToken"] = token };
            var response = await client.PostAsync(
                $"/admin/productos/EliminarImagen/{ids.ProductoId + 99}/{ids.FilaId}",
                new FormUrlEncodedContent(form));

            // El guard lanza ImagenNoValidaException y el filtro global la vuelve
            // redirect con mensaje (nunca el stack).
            Assert.Equal(HttpStatusCode.Found, response.StatusCode);
            var fila = _app.WithDb(ctx => ctx.ImagenesProducto.AsNoTracking()
                .SingleOrDefault(i => i.ProductoImagenId == ids.FilaId));
            Assert.NotNull(fila);
            _app.Imagen.Verify(x => x.BorrarImagenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}

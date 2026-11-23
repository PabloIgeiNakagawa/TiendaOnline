using TiendaOnline.Application.Auditoria;
using TiendaOnline.Tests.TestSupport;
using Xunit;
using ServicioDeAuditoria = TiendaOnline.Infrastructure.Services.Auditoria.AuditoriaService;

namespace TiendaOnline.Tests.Services.Auditoria
{
    /// <summary>
    /// Panel de auditoría: filtros por entidad (historial desde producto/pedido/usuario),
    /// búsqueda libre que también alcanza el id y filas de sistema sin usuario.
    /// </summary>
    public class AuditoriaServiceTests
    {
        private readonly TestDatabase _db = new();

        [Fact]
        public async Task ObtenerAuditoriasPaginadas_FiltraPorEntidad()
        {
            using (var seed = _db.CreateContext())
            {
                var admin = TestDatabase.SeedUsuario(seed);
                TestDatabase.SeedAuditoria(seed, admin, "Envió Pedido", "Pedido", "1");
                TestDatabase.SeedAuditoria(seed, admin, "Entregó Pedido", "Pedido", "2");
                TestDatabase.SeedAuditoria(seed, admin, "Actualizó Usuario", "Usuario", "1");
            }

            using var context = _db.CreateContext();
            var servicio = new ServicioDeAuditoria(context);

            var resultado = await servicio.ObtenerAuditoriasPaginadasAsync(new ObtenerLogsRequest
            {
                TablaAfectada = "Pedido",
                EntidadId = "1"
            });

            var fila = Assert.Single(resultado.Items);
            Assert.Equal("Envió Pedido", fila.Accion);
            Assert.Equal("Pedido", fila.TablaAfectada);
            Assert.Equal("1", fila.EntidadId);
        }

        [Fact]
        public async Task ObtenerAuditoriasPaginadas_SinUsuario_MuestraSistema()
        {
            using (var seed = _db.CreateContext())
            {
                TestDatabase.SeedAuditoria(seed, usuario: null,
                    accion: "Sistema: Canceló Pedido por vencimiento", entidadId: "77");
            }

            using var context = _db.CreateContext();
            var resultado = await new ServicioDeAuditoria(context)
                .ObtenerAuditoriasPaginadasAsync(new ObtenerLogsRequest());

            var fila = Assert.Single(resultado.Items);
            Assert.Equal("Sistema", fila.UsuarioNombreCompleto);
            Assert.Null(fila.UsuarioEmail);
        }

        [Fact]
        public async Task ObtenerAuditoriasPaginadas_BusquedaLibre_EncuentraPorEntidadId()
        {
            using (var seed = _db.CreateContext())
            {
                var admin = TestDatabase.SeedUsuario(seed);
                TestDatabase.SeedAuditoria(seed, admin, "Envió Pedido", "Pedido", "123");
                TestDatabase.SeedAuditoria(seed, admin, "Envió Pedido", "Pedido", "456");
            }

            using var context = _db.CreateContext();
            var resultado = await new ServicioDeAuditoria(context)
                .ObtenerAuditoriasPaginadasAsync(new ObtenerLogsRequest { Busqueda = "123" });

            Assert.Equal("123", Assert.Single(resultado.Items).EntidadId);
        }

        [Fact]
        public async Task ObtenerAuditoriasParaExportar_RespetaLosMismosFiltrosDelListado()
        {
            using (var seed = _db.CreateContext())
            {
                var admin = TestDatabase.SeedUsuario(seed);
                TestDatabase.SeedAuditoria(seed, admin, "Envió Pedido", "Pedido", "5",
                    fecha: DateTime.Now.AddHours(-2));
                TestDatabase.SeedAuditoria(seed, admin, "Entregó Pedido", "Pedido", "5",
                    fecha: DateTime.Now.AddHours(-1));
                TestDatabase.SeedAuditoria(seed, admin, "Envió Pedido", "Pedido", "6");
            }

            using var context = _db.CreateContext();
            var servicio = new ServicioDeAuditoria(context);

            var filas = await servicio.ObtenerAuditoriasParaExportarAsync(new ObtenerLogsRequest
            {
                TablaAfectada = "Pedido",
                EntidadId = "5"
            });

            Assert.Equal(2, filas.Count);
            Assert.All(filas, f => Assert.Equal("5", f.EntidadId));
            // Más reciente primero, igual que el listado.
            Assert.Equal("Entregó Pedido", filas[0].Accion);
        }

        [Fact]
        public async Task ObtenerDetalle_DevuelveAccionYEntidadParaElModal()
        {
            int id;
            using (var seed = _db.CreateContext())
            {
                id = TestDatabase.SeedAuditoria(seed,
                    accion: "Entregó Pedido",
                    tablaAfectada: "Pedido",
                    entidadId: "42",
                    datosAnteriores: "{\"Estado\":\"Enviado\"}",
                    datosNuevos: "{\"Estado\":\"Entregado\"}").AuditoriaId;
            }

            using var context = _db.CreateContext();
            var detalle = await new ServicioDeAuditoria(context).ObtenerDetalleAuditoriaAsync(id);

            Assert.NotNull(detalle);
            Assert.Equal("Entregó Pedido", detalle!.Accion);
            Assert.Equal("Pedido", detalle.TablaAfectada);
            Assert.Equal("42", detalle.EntidadId);
            Assert.Contains("Enviado", detalle.DatosAnteriores);
            Assert.Contains("Entregado", detalle.DatosNuevos);
        }
    }
}

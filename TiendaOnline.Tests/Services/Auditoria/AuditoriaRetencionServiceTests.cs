using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TiendaOnline.Application.Common.Settings;
using TiendaOnline.Tests.TestSupport;
using Xunit;
using ServicioDeRetencion = TiendaOnline.Infrastructure.Services.Auditoria.AuditoriaRetencionService;
using EntidadAuditoria = TiendaOnline.Domain.Entities.Auditoria;

namespace TiendaOnline.Tests.Services.Auditoria
{
    /// <summary>
    /// Purga mensual del historial: ventana configurable, default ante una
    /// configuración inválida y borrado en lotes.
    /// </summary>
    public class AuditoriaRetencionServiceTests
    {
        private readonly TestDatabase _db = new();

        [Fact]
        public async Task Purgar_BorraSoloLasFilasAnterioresALaRetencion()
        {
            using (var seed = _db.CreateContext())
            {
                TestDatabase.SeedAuditoria(seed, fecha: DateTime.Now.AddMonths(-25));
                TestDatabase.SeedAuditoria(seed, fecha: DateTime.Now.AddMonths(-23));
                TestDatabase.SeedAuditoria(seed, fecha: DateTime.Now);
            }

            using var context = _db.CreateContext();
            var borradas = await CrearServicio(context, 24).PurgarAuditoriasAsync();

            Assert.Equal(1, borradas);
            using var verificacion = _db.CreateContext();
            Assert.Equal(2, verificacion.Auditorias.Count());
        }

        [Fact]
        public async Task Purgar_RetencionInvalida_UsaElDefaultDe24Meses()
        {
            using (var seed = _db.CreateContext())
            {
                TestDatabase.SeedAuditoria(seed, fecha: DateTime.Now.AddMonths(-30));
                TestDatabase.SeedAuditoria(seed, fecha: DateTime.Now.AddMonths(-2));
            }

            using var context = _db.CreateContext();
            var borradas = await CrearServicio(context, 0).PurgarAuditoriasAsync();

            Assert.Equal(1, borradas);
            using var verificacion = _db.CreateContext();
            Assert.Equal(1, verificacion.Auditorias.Count());
        }

        [Fact]
        public async Task Purgar_MasFilasQueElLote_BorraTodo()
        {
            var cantidad = ServicioDeRetencion.TamanoLote + 50;

            using (var seed = _db.CreateContext())
            {
                var viejas = Enumerable.Range(0, cantidad)
                    .Select(_ => new EntidadAuditoria
                    {
                        Accion = "Actualizó Pedido",
                        TablaAfectada = "Pedido",
                        EntidadId = "1",
                        DatosAnteriores = "{}",
                        DatosNuevos = "{}",
                        Fecha = DateTime.Now.AddYears(-3)
                    });

                seed.Auditorias.AddRange(viejas);
                seed.SaveChanges();
            }

            using var context = _db.CreateContext();
            var borradas = await CrearServicio(context, 24).PurgarAuditoriasAsync();

            Assert.Equal(cantidad, borradas);
            using var verificacion = _db.CreateContext();
            Assert.Equal(0, verificacion.Auditorias.Count());
        }

        private static ServicioDeRetencion CrearServicio(
            TiendaOnline.Infrastructure.Persistence.TiendaContext context,
            int meses)
        {
            return new ServicioDeRetencion(
                context,
                Options.Create(new AuditoriaSettings { RetencionMeses = meses }),
                NullLogger<ServicioDeRetencion>.Instance);
        }
    }
}

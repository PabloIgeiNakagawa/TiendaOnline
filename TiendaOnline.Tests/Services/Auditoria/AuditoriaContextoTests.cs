using Moq;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Services.Auditoria;
using TiendaOnline.Tests.TestSupport;
using Xunit;

namespace TiendaOnline.Tests.Services.Auditoria
{
    /// <summary>
    /// Contexto del request en las filas de auditoría: IP, agente, traza y origen
    /// llegan a la tabla por las dos vías (captura automática y writer explícito);
    /// fuera de un request el origen queda en Sistema.
    /// </summary>
    public class AuditoriaContextoTests
    {
        private readonly TestDatabase _db = new();

        [Fact]
        public async Task Guardar_ConContextoDeRequest_CompletaLasColumnas()
        {
            var contexto = ContextoDeRequest();

            using (var ctx = _db.CreateContext(auditoriaContexto: contexto.Object))
            {
                ctx.Categorias.Add(new Categoria { Nombre = "Contexto" });
                await ctx.SaveChangesAsync();
            }

            using var verificacion = _db.CreateContext();
            var fila = Assert.Single(verificacion.Auditorias
                .Where(a => a.TablaAfectada == nameof(Categoria)));

            Assert.Equal("203.0.113.10", fila.Ip);
            Assert.Equal("Mozilla/5.0 (test)", fila.UserAgent);
            Assert.Equal("traza-123", fila.CorrelationId);
            Assert.Equal("Web", fila.Origen);
        }

        [Fact]
        public async Task Guardar_SinContexto_MarcaOrigenSistema()
        {
            using (var ctx = _db.CreateContext())
            {
                ctx.Categorias.Add(new Categoria { Nombre = "SinContexto" });
                await ctx.SaveChangesAsync();
            }

            using var verificacion = _db.CreateContext();
            var fila = Assert.Single(verificacion.Auditorias
                .Where(a => a.TablaAfectada == nameof(Categoria)));

            Assert.Equal("Sistema", fila.Origen);
            Assert.Null(fila.Ip);
            Assert.Null(fila.UserAgent);
            Assert.Null(fila.CorrelationId);
        }

        [Fact]
        public async Task Guardar_UserAgentMasLargoQueLaColumna_SeRecorta()
        {
            var contexto = ContextoDeRequest();
            contexto.SetupGet(c => c.UserAgent).Returns(new string('x', 600));

            using (var ctx = _db.CreateContext(auditoriaContexto: contexto.Object))
            {
                ctx.Categorias.Add(new Categoria { Nombre = "Contexto" });
                await ctx.SaveChangesAsync();
            }

            using var verificacion = _db.CreateContext();
            var fila = Assert.Single(verificacion.Auditorias
                .Where(a => a.TablaAfectada == nameof(Categoria)));

            Assert.Equal(TiendaOnline.Infrastructure.Persistence.TiendaContext.LargoMaximoUserAgent, fila.UserAgent!.Length);
        }

        [Fact]
        public async Task Writer_ConContextoDeRequest_CompletaLasColumnas()
        {
            var contexto = ContextoDeRequest();

            using (var ctx = _db.CreateContext(auditoriaContexto: contexto.Object))
            {
                var usuario = TestDatabase.SeedUsuario(ctx);
                var sesion = Mock.Of<IUserSessionService>(s => s.GetUserId() == usuario.UsuarioId);
                new AuditoriaWriter(ctx, sesion).Registrar(
                    "Envió Pedido",
                    nameof(Pedido),
                    "10",
                    new { Estado = EstadoPedido.EnPreparacion },
                    new { Estado = EstadoPedido.Enviado });

                await ctx.SaveChangesAsync();
            }

            using var verificacion = _db.CreateContext();
            var fila = Assert.Single(verificacion.Auditorias);

            Assert.Equal("203.0.113.10", fila.Ip);
            Assert.Equal("traza-123", fila.CorrelationId);
            Assert.Equal("Web", fila.Origen);
        }

        private static Mock<IAuditoriaContexto> ContextoDeRequest()
        {
            var contexto = new Mock<IAuditoriaContexto>();
            contexto.SetupGet(c => c.Ip).Returns("203.0.113.10");
            contexto.SetupGet(c => c.UserAgent).Returns("Mozilla/5.0 (test)");
            contexto.SetupGet(c => c.CorrelationId).Returns("traza-123");
            contexto.SetupGet(c => c.Origen).Returns("Web");
            return contexto;
        }
    }
}

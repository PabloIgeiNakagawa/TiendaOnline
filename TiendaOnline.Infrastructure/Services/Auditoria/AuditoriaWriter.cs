using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Infrastructure.Persistence;
using EntidadAuditoria = TiendaOnline.Domain.Entities.Auditoria;

namespace TiendaOnline.Infrastructure.Services.Auditoria
{
    /// <summary>
    /// Filas de auditoría para las transiciones que escribe ExecuteUpdateAsync y que
    /// por eso no ve el ChangeTracker. Comparte la transacción del caller: la fila
    /// se agrega al mismo contexto y la guarda su SaveChanges.
    /// </summary>
    public class AuditoriaWriter : IAuditoriaWriter
    {
        private readonly TiendaContext _context;
        private readonly IUserSessionService _userSession;

        public AuditoriaWriter(TiendaContext context, IUserSessionService userSession)
        {
            _context = context;
            _userSession = userSession;
        }

        public void Registrar(
            string accion,
            string tablaAfectada,
            string entidadId,
            object datosAnteriores,
            object datosNuevos)
        {
            var usuarioId = _userSession.GetUserId();

            _context.Auditorias.Add(new EntidadAuditoria
            {
                UsuarioId = usuarioId,
                TablaAfectada = tablaAfectada,
                // Misma convención que la captura automática: sin sesión (jobs de
                // Hangfire, webhooks) la acción queda marcada como Sistema.
                Accion = usuarioId is null ? $"Sistema: {accion}" : accion,
                EntidadId = entidadId,
                DatosAnteriores = AuditoriaJson.Serializar(datosAnteriores),
                DatosNuevos = AuditoriaJson.Serializar(datosNuevos),
                Fecha = DateTime.Now
            });
        }
    }
}

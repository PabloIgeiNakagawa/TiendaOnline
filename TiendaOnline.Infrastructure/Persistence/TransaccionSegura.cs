using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace TiendaOnline.Infrastructure.Persistence
{
    /// <summary>
    /// Revierte la transacción sin tapar el error real: cuando la excepción salta
    /// en un paso posterior al commit (recuperar el pedido, encolar el email) la
    /// transacción ya está terminada y el rollback lanza por sí solo. Esa falla
    /// del rollback va al log y el error original sigue intacto hacia el caller.
    /// </summary>
    internal static class TransaccionSegura
    {
        public static async Task RevertirAsync(IDbContextTransaction? transaction, ILogger logger, CancellationToken ct)
        {
            if (transaction is null) return;

            try
            {
                await transaction.RollbackAsync(ct);
            }
            catch (Exception rollback)
            {
                logger.LogWarning(rollback, "Rollback no ejecutable: la transacción ya estaba terminada.");
            }
        }
    }
}

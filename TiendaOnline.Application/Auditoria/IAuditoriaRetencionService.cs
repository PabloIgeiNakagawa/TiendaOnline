namespace TiendaOnline.Application.Auditoria
{
    /// <summary>
    /// Purga mensual del historial de auditoría: borra las filas más viejas que
    /// la retención configurada. No se audita a sí misma (la tabla está excluida).
    /// </summary>
    public interface IAuditoriaRetencionService
    {
        Task<int> PurgarAuditoriasAsync();
    }
}

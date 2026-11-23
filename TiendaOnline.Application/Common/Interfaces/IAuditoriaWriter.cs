namespace TiendaOnline.Application.Common.Interfaces
{
    /// <summary>
    /// Registra una fila de auditoría para un cambio que no pasó por el ChangeTracker
    /// (transiciones de pedido con ExecuteUpdateAsync). No guarda por su cuenta:
    /// agrega la fila al contexto y la persiste el SaveChanges del caller, dentro de
    /// su misma transacción — el cambio de negocio y su registro quedan atómicos.
    /// La atribución y el prefijo "Sistema:" los resuelve la implementación con la
    /// sesión vigente, igual que la captura automática.
    /// </summary>
    public interface IAuditoriaWriter
    {
        void Registrar(
            string accion,
            string tablaAfectada,
            string entidadId,
            object datosAnteriores,
            object datosNuevos);
    }
}

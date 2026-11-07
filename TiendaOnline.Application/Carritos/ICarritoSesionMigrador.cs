namespace TiendaOnline.Application.Carritos
{
    /// <summary>
    /// Movida de carrito al loguear: los items que el anónimo venía armando en su
    /// session pasan al carrito guardado en BD de la cuenta con la que entra.
    /// </summary>
    public interface ICarritoSesionMigrador
    {
        /// <summary>True si había carrito de sesión y quedó fusionado; false si la session estaba vacía.</summary>
        Task<bool> FusionarConCarritoGuardadoAsync(int usuarioId, CancellationToken ct = default);
    }
}

namespace TiendaOnline.Application.Productos.Commands
{
    public interface IProductoCommandService
    {
        Task AgregarProductoAsync(AgregarProductoDto dto, CancellationToken ct = default);
        Task EditarProductoAsync(EditarProductoDto dto, CancellationToken ct = default);
        Task EliminarImagenAsync(int productoId, int imagenId, CancellationToken ct = default);
        Task DarAltaProductoAsync(int productoId, CancellationToken ct = default);
        Task DarBajaProductoAsync(int productoId, CancellationToken ct = default);
    }
}
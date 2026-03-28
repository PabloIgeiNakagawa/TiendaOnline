namespace TiendaOnline.Application.Productos.Commands
{
    public interface IProductoCommandService
    {
        Task AgregarProductoAsync(AgregarProductoDto dto);
        Task EditarProductoAsync(EditarProductoDto dto);
        Task DarAltaProductoAsync(int productoId);
        Task DarBajaProductoAsync(int productoId);
    }
}
namespace TiendaOnline.Application.Carritos
{
    public interface ICarritoService
    {
        Task<List<ItemCarrito>> ObtenerAsync();
        Task<int> ObtenerCantidadTotalAsync();
        Task AgregarAsync(int productoId);
        Task EliminarAsync(int productoId);
        Task ActualizarCantidadAsync(int productoId, int cantidad);
        Task VaciarAsync();
    }
}

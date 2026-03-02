namespace TiendaOnline.Application.Carritos
{
    public interface ICarritoStorage
    {
        Task<List<ItemCarrito>> ObtenerAsync();
        Task GuardarAsync(List<ItemCarrito> items);
        Task LimpiarAsync();
    }
}

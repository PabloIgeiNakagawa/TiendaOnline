using TiendaOnline.Application.Common;

namespace TiendaOnline.Application.Carritos
{
    public interface ICarritoService
    {
        Task<List<ItemCarrito>> ObtenerAsync();
        Task<int> ObtenerCantidadTotalAsync();
        Task<Result> AgregarAsync(int productoId);
        Task<Result> EliminarAsync(int productoId);
        Task<Result> ActualizarCantidadAsync(int productoId, int cantidad);
        Task<Result<ValidacionStockDto>> ValidarStockAsync();
        Task VaciarAsync();
    }

    public class ValidacionStockDto
    {
        public bool TodoOK { get; set; }
        public List<StockInsuficienteDto> ItemsSinStock { get; set; } = new();
    }

    public class StockInsuficienteDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int CantidadSolicitada { get; set; }
        public int StockDisponible { get; set; }
    }
}

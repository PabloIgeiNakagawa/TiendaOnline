namespace TiendaOnline.Application.Ofertas
{
    /// <summary>
    /// <summary>
    /// Lecturas de ofertas de productos. El precio efectivo con el que se cobra sale acá:
    /// mejor oferta vigente ?? precio de lista.
    /// </summary>
    /// </summary>
    public interface IOfertaQueryService
    {
        /// <summary>
        /// Mejor oferta vigente del producto (Activa con ventana FechaInicio-FechaFin actual).
        /// Si hay varias solapadas gana la más barata. Null si no hay ninguna.
        /// </summary>
        Task<OfertaVigenteDto?> ObtenerOfertaVigenteAsync(int productoId, CancellationToken ct = default);

        /// <summary>
        /// Mejor oferta vigente por producto, una sola query (listados: evita N+1).
        /// Los ids sin oferta vigente no tienen entrada en el diccionario.
        /// </summary>
        Task<Dictionary<int, OfertaVigenteDto>> ObtenerOfertasVigentesAsync(IEnumerable<int> productoIds, CancellationToken ct = default);

        /// <summary>Listado completo para el panel admin (con estado calculado: Vigente/Futura/Expirada/Inactiva).</summary>
        Task<List<OfertaDto>> ObtenerTodasAsync(CancellationToken ct = default);

        /// <summary>Productos activos con su precio de lista (select del ABM de ofertas).</summary>
        Task<List<ProductoParaOfertaDto>> ObtenerProductosParaOfertaAsync(CancellationToken ct = default);
    }

    /// <summary>Precio de oferta a aplicar en el punto exacto del cobro.</summary>
    public class OfertaVigenteDto
    {
        public int OfertaId { get; set; }
        public int ProductoId { get; set; }
        public decimal PrecioOferta { get; set; }
        /// <summary>Fin de vigencia: permite al cliente ver "hasta cuándo" vale la promo.</summary>
        public DateTime FechaFin { get; set; }
    }

    /// <summary>Fila del listado de ofertas del panel admin.</summary>
    public class OfertaDto
    {
        public int OfertaId { get; set; }
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public decimal PrecioLista { get; set; }
        public decimal PrecioOferta { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activa { get; set; }
        /// <summary>Vigente · Futura · Expirada · Inactiva (calculado en memoria, no en la DB).</summary>
        public string Estado { get; set; } = string.Empty;
    }

    /// <summary>Producto habilitado para recibir una oferta (select del ABM).</summary>
    public class ProductoParaOfertaDto
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal PrecioLista { get; set; }
    }
}

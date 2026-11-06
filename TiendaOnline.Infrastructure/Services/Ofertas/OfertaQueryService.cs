using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Ofertas
{
    public class OfertaQueryService : IOfertaQueryService
    {
        private readonly TiendaContext _context;

        public OfertaQueryService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<OfertaVigenteDto?> ObtenerOfertaVigenteAsync(int productoId, CancellationToken ct = default)
        {
            var ofertas = await ObtenerOfertasVigentesAsync(new[] { productoId }, ct);
            return ofertas.TryGetValue(productoId, out var oferta) ? oferta : null;
        }

        public async Task<Dictionary<int, OfertaVigenteDto>> ObtenerOfertasVigentesAsync(IEnumerable<int> productoIds, CancellationToken ct = default)
        {
            var ids = productoIds as List<int> ?? productoIds.ToList();
            var resultado = new Dictionary<int, OfertaVigenteDto>();

            if (ids.Count == 0)
                return resultado;

            var ahora = DateTime.Now;

            // Punto único de decisión de precio: de las ofertas activas
            // con vigencia corriente gana la más barata por producto.
            var vigentes = await _context.OfertasProducto
                .AsNoTracking()
                .Where(o => ids.Contains(o.ProductoId)
                            && o.Activa
                            && o.FechaInicio <= ahora
                            && o.FechaFin >= ahora)
                .ToListAsync(ct);

            foreach (var grupo in vigentes.GroupBy(o => o.ProductoId))
            {
                var mejor = grupo.OrderBy(o => o.PrecioOferta).First();
                resultado[grupo.Key] = new OfertaVigenteDto
                {
                    OfertaId = mejor.OfertaId,
                    ProductoId = mejor.ProductoId,
                    PrecioOferta = mejor.PrecioOferta,
                    FechaFin = mejor.FechaFin
                };
            }

            return resultado;
        }

        public async Task<List<ProductoParaOfertaDto>> ObtenerProductosParaOfertaAsync(CancellationToken ct = default)
        {
            return await _context.Productos
                .AsNoTracking()
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .Select(p => new ProductoParaOfertaDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    PrecioLista = p.Precio
                })
                .ToListAsync(ct);
        }

        public async Task<List<OfertaDto>> ObtenerTodasAsync(CancellationToken ct = default)
        {
            var ahora = DateTime.Now;

            var ofertas = await _context.OfertasProducto
                .AsNoTracking()
                .OrderByDescending(o => o.FechaInicio)
                .ThenByDescending(o => o.OfertaId)
                .Select(o => new OfertaDto
                {
                    OfertaId = o.OfertaId,
                    ProductoId = o.ProductoId,
                    NombreProducto = o.Producto.Nombre,
                    PrecioLista = o.Producto.Precio,
                    PrecioOferta = o.PrecioOferta,
                    FechaInicio = o.FechaInicio,
                    FechaFin = o.FechaFin,
                    Activa = o.Activa
                })
                .ToListAsync(ct);

            // El estado es regla de presentación: no se puede traducir a SQL
            foreach (var oferta in ofertas)
            {
                oferta.Estado = !oferta.Activa ? "Inactiva"
                    : oferta.FechaFin < ahora ? "Expirada"
                    : oferta.FechaInicio > ahora ? "Futura"
                    : "Vigente";
            }

            return ofertas;
        }
    }
}

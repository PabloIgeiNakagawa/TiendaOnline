using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Ofertas
{
    public class OfertaCommandService : IOfertaCommandService
    {
        private readonly TiendaContext _context;

        public OfertaCommandService(TiendaContext context)
        {
            _context = context;
        }

        public async Task CrearOfertaAsync(CrearOfertaDto dto, CancellationToken ct = default)
        {
            var producto = await ValidarYObtenerProductoAsync(dto, ct);

            _context.OfertasProducto.Add(new OfertaProducto
            {
                ProductoId = producto.ProductoId,
                PrecioOferta = dto.PrecioOferta,
                FechaInicio = dto.FechaInicio,
                FechaFin = dto.FechaFin,
                Activa = true
            });

            await _context.SaveChangesAsync(ct);
        }

        public async Task EditarOfertaAsync(int ofertaId, CrearOfertaDto dto, CancellationToken ct = default)
        {
            var oferta = await _context.OfertasProducto.FirstOrDefaultAsync(o => o.OfertaId == ofertaId, ct);
            if (oferta is null)
                throw new OfertaNoValidaException("La oferta que intentás editar no existe.");

            // En edición el producto no se cambia (el ABM muta la promo, no reasigna el producto).
            var producto = await _context.Productos.AsNoTracking()
                .SingleAsync(p => p.ProductoId == oferta.ProductoId, ct);

            ValidarReglas(dto, producto);

            oferta.PrecioOferta = dto.PrecioOferta;
            oferta.FechaInicio = dto.FechaInicio;
            oferta.FechaFin = dto.FechaFin;

            await _context.SaveChangesAsync(ct);
        }

        public async Task AlternarActivaAsync(int ofertaId, CancellationToken ct = default)
        {
            var oferta = await _context.OfertasProducto.FirstOrDefaultAsync(o => o.OfertaId == ofertaId, ct);
            if (oferta is null)
                throw new OfertaNoValidaException("La oferta no existe.");

            oferta.Activa = !oferta.Activa;
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>Alta: valida producto (existe y activo) + reglas de la promo.</summary>
        private async Task<Producto> ValidarYObtenerProductoAsync(CrearOfertaDto dto, CancellationToken ct)
        {
            var producto = await _context.Productos.AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProductoId == dto.ProductoId, ct);
            if (producto is null)
                throw new OfertaNoValidaException("No existe el producto indicado.");

            if (!producto.Activo)
                throw new OfertaNoValidaException("El producto está inactivo: no se le puede crear una oferta.");

            ValidarReglas(dto, producto);
            return producto;
        }

        private static void ValidarReglas(CrearOfertaDto dto, Producto producto)
        {
            if (dto.FechaFin <= dto.FechaInicio)
                throw new OfertaNoValidaException("La fecha de fin debe ser posterior a la fecha de inicio.");

            if (dto.PrecioOferta <= 0)
                throw new OfertaNoValidaException("El precio de oferta debe ser mayor a 0.");

            if (dto.PrecioOferta >= producto.Precio)
                throw new OfertaNoValidaException("El precio de oferta debe ser menor al precio de lista del producto.");
        }
    }
}

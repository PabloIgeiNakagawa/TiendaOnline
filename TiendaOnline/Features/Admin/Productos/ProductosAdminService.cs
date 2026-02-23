using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Features.Shared.Models;

namespace TiendaOnline.Features.Admin.Productos
{
    public class ProductosAdminService : IProductosAdminService
    {
        private readonly TiendaContext _context;
        private readonly IImagenService _imagenService;

        public ProductosAdminService(TiendaContext context, IImagenService imagenService)
        {
            _context = context;
            _imagenService = imagenService;
        }

        public async Task<ProductoDetalleDto?> ObtenerProductoAsync(int id)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .Select(p => new ProductoDetalleDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Descripcion = p.Descripcion,
                    Precio = p.Precio,
                    Stock = p.Stock,
                    CategoriaId = p.CategoriaId,
                    NombreCategoria = p.Categoria.Nombre,
                    ImagenUrl = p.ImagenUrl
                })
                .FirstOrDefaultAsync(p => p.ProductoId == id);
        }

        public async Task<PagedResult<ProductoListaDto>> ObtenerProductosPaginadosAsync(string? busqueda, int? categoriaId, string? estado, string? stock, int pagina, int registrosPorPagina)
        {
            var query = _context.Productos
                .AsNoTracking()
                .Include(p => p.Categoria)
                .AsQueryable();

            // --- Filtros ---
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(p => p.Nombre.Contains(busqueda) || p.Descripcion.Contains(busqueda));
            }

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.CategoriaId == categoriaId);
            }

            if (!string.IsNullOrEmpty(estado))
            {
                query = query.Where(p => estado == "activo" ? p.Activo : !p.Activo);
            }

            if (!string.IsNullOrEmpty(stock))
            {
                query = stock switch
                {
                    "agotado" => query.Where(p => p.Stock == 0),
                    "bajo" => query.Where(p => p.Stock > 0 && p.Stock <= 5),
                    "disponible" => query.Where(p => p.Stock > 5),
                    _ => query
                };
            }

            // --- Paginación ---
            int totalElementos = await query.CountAsync();

            var items = await query
                .OrderByDescending(p => p.ProductoId) // Siempre ordenar para paginar
                .Skip((pagina - 1) * registrosPorPagina)
                .Take(registrosPorPagina)
                .Select(p => new ProductoListaDto
                {
                    ProductoId = p.ProductoId,
                    Nombre = p.Nombre,
                    Precio = p.Precio,
                    Stock = p.Stock,
                    ImagenUrl = p.ImagenUrl,
                    Activo = p.Activo,
                    CategoriaNombre = p.Categoria != null ? p.Categoria.Nombre : "Sin categoría",
                    CategoriaId = p.CategoriaId
                })
                .ToListAsync();

            return new PagedResult<ProductoListaDto>(items, totalElementos, pagina, registrosPorPagina);
        }

        public async Task AgregarProductoAsync(AgregarProductoDto dto)
        {
            string urlImagen = "/img/no-image.png";

            if (dto.ImagenStream != null)
            {
                urlImagen = await _imagenService.SubirImagenAsync(dto.ImagenStream, dto.NombreArchivo);
            }

            var nuevoProducto = new Producto
            {
                Nombre = dto.Nombre,
                Descripcion = dto.Descripcion,
                Precio = dto.Precio,
                Stock = dto.Stock,
                CategoriaId = dto.CategoriaId,
                ImagenUrl = urlImagen,
                Activo = true
            };

            _context.Productos.Add(nuevoProducto);
            await _context.SaveChangesAsync();
        }

        public async Task DarAltaProductoAsync(int productoId)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) throw new Exception("Producto no encontrado.");

            var estadoAnterior = new { producto.ProductoId, producto.Nombre, producto.Activo };

            producto.Activo = true;

            await _context.SaveChangesAsync();
        }

        public async Task DarBajaProductoAsync(int productoId)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) throw new Exception("Producto no encontrado.");

            var estadoAnterior = new { producto.ProductoId, producto.Nombre, producto.Activo };

            producto.Activo = false;

            await _context.SaveChangesAsync();
        }

        public async Task EditarProductoAsync(EditarProductoDto dto)
        {
            var producto = await _context.Productos.FindAsync(dto.ProductoId);
            if (producto == null) throw new KeyNotFoundException("Producto no encontrado.");

            if (dto.ImagenStream != null)
            {
                // Borrar la vieja si existe
                if (!string.IsNullOrEmpty(producto.ImagenUrl) && !producto.ImagenUrl.Contains("no-image.png"))
                {
                    string publicIdViejo = _imagenService.ExtraerPublicIdDesdeUrl(producto.ImagenUrl);
                    await _imagenService.BorrarImagenAsync(publicIdViejo);
                }

                // Subir la nueva
                producto.ImagenUrl = await _imagenService.SubirImagenAsync(dto.ImagenStream, dto.NombreArchivo ?? "producto");
            }

            // Actualización de campos
            producto.Nombre = dto.Nombre;
            producto.Descripcion = dto.Descripcion;
            producto.Precio = dto.Precio;
            producto.CategoriaId = dto.CategoriaId;

            await _context.SaveChangesAsync();
        }
    }
}

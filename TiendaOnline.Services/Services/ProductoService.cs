using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Services.IServices;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Services.Commons.Models;
using TiendaOnline.Services.DTOs.Admin.Producto;

namespace TiendaOnline.Services.Services
{
    public class ProductoService : IProductoService
    {
        private readonly TiendaContext _context;
        private readonly IImagenService _imagenService;

        public ProductoService(TiendaContext context, IImagenService imagenService)
        {
            _context = context;
            _imagenService = imagenService;
        }

        public async Task<Producto?> ObtenerProductoAsync(int id)
        {
            return await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.ProductoId == id);
        }

        public async Task<List<Producto>> ObtenerProductosAsync()
        {
            return await _context.Productos
                                 .AsNoTracking() // Solo lectura: mucho más rápido y ligero
                                 .Include(p => p.Categoria)
                                 .ToListAsync();
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


        public async Task AgregarProductoAsync(Producto producto)
        {
            _context.Productos.Add(producto);
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

        public async Task DarAltaProductoAsync(int productoId)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) throw new Exception("Producto no encontrado.");

            var estadoAnterior = new { producto.ProductoId, producto.Nombre, producto.Activo };

            producto.Activo = true;

            await _context.SaveChangesAsync();
        }

        public async Task EditarProductoAsync(int productoId, Producto productoEditado, Stream? archivoStream, string? nombreArchivo)
        {
            var producto = await _context.Productos.FindAsync(productoId);
            if (producto == null) throw new Exception("Producto no encontrado.");

            if (archivoStream != null && archivoStream.CanRead)
            {
                if (!string.IsNullOrEmpty(producto.ImagenUrl))
                {
                    string publicIdViejo = _imagenService.ExtraerPublicIdDesdeUrl(producto.ImagenUrl);
                    await _imagenService.BorrarImagenAsync(publicIdViejo);
                }

                string nuevaUrl = await _imagenService.SubirImagenAsync(archivoStream, nombreArchivo ?? "producto_imagen");
                producto.ImagenUrl = nuevaUrl;
            }

            producto.Nombre = productoEditado.Nombre;
            producto.Descripcion = productoEditado.Descripcion;
            producto.Precio = productoEditado.Precio;
            producto.Stock = productoEditado.Stock;
            producto.CategoriaId = productoEditado.CategoriaId;

            await _context.SaveChangesAsync();
        }
    }
}

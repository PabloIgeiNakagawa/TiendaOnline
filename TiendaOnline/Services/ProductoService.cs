using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;

public class ProductoService : IProductoService
{
    private readonly TiendaContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly IImagenService _imagenService;

    public ProductoService(TiendaContext context, IAuditoriaService auditoriaService, IImagenService imagenService)
    {
        _context = context;
        _auditoriaService = auditoriaService;
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

    public async Task EditarProductoAsync(int productoId, Producto productoEditado, IFormFile ImagenArchivo)
    {
        var producto = await _context.Productos.FindAsync(productoId);
        if (producto == null) throw new Exception("Producto no encontrado.");

        var datosAnteriores = new
        {
            producto.Nombre,
            producto.Descripcion,
            producto.Precio,
            producto.Stock,
            producto.CategoriaId,
            producto.ImagenUrl
        };

        if (ImagenArchivo != null && ImagenArchivo.Length > 0)
        {
            if (!string.IsNullOrEmpty(producto.ImagenUrl))
            {
                string publicIdViejo = _imagenService.ExtraerPublicIdDesdeUrl(producto.ImagenUrl);
                await _imagenService.BorrarImagenAsync(publicIdViejo);
            }

            string nuevaUrl = await _imagenService.SubirImagenAsync(ImagenArchivo);
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

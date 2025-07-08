using MercadoPago.Resource.User;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;

public class ProductoService : IProductoService
{
    private readonly TiendaContext _context;
    private readonly IAuditoriaService _auditoriaService;
    private readonly int usuarioActivoId;

    public ProductoService(TiendaContext context, IAuditoriaService auditoriaService)
    {
        _context = context;
        _auditoriaService = auditoriaService;
        usuarioActivoId = _auditoriaService.ObtenerUsuarioId(); // Obtener el usuario actual para auditoría
    }

    public async Task<Producto?> ObtenerProductoAsync(int id)
    {
        var producto = await _context.Productos.FindAsync(id);
        if(producto == null)
        {
            return null;
        }
        var categoria = await _context.Categorias.FindAsync(producto.CategoriaId);
        return producto;
    }

    public async Task<List<Producto>> ObtenerProductosAsync()
    {
        return await _context.Productos
                             .Include(p => p.Categoria)
                             .ToListAsync();
    }

    public async Task AgregarProductoAsync(Producto producto)
    {
        _context.Productos.Add(producto);
        int cambios = await _context.SaveChangesAsync();

        if(cambios > 0)
        {
            var auditoria = new Auditoria
            {
                UsuarioId = usuarioActivoId,
                Accion = "Agregar Producto",
                DatosAnteriores = "{}",
                DatosNuevos = JsonConvert.SerializeObject(producto),
                Fecha = DateTime.Now
            };
            await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
        }
        else
        {
            throw new Exception("No se pudo agregar el producto.");
        }
    }

    public async Task DarBajaProductoAsync(int productoId)
    {
        var producto = await _context.Productos.FindAsync(productoId);
        if (producto == null)
            throw new Exception("Producto no encontrado.");

        producto.Activo = false;

        int cambios = await _context.SaveChangesAsync();
        if (cambios > 0)
        {
            var auditoria = new Auditoria
            {
                UsuarioId = usuarioActivoId,
                Accion = "Dar de baja producto",
                DatosAnteriores = JsonConvert.SerializeObject(new { Activo = true }),
                DatosNuevos = JsonConvert.SerializeObject(new { Activo = false }),
                Fecha = DateTime.Now
            };
            await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
        }
        else
        {
            throw new Exception("No se pudo dar de baja el producto.");
        }
    }

    public async Task DarAltaProductoAsync(int productoId)
    {
        var producto = await _context.Productos.FindAsync(productoId);
        if (producto == null)
            throw new Exception("Producto no encontrado.");

        producto.Activo = true;

        int cambios = await _context.SaveChangesAsync();
        if (cambios > 0)
        {
            var auditoria = new Auditoria
            {
                UsuarioId = usuarioActivoId,
                Accion = "Dar de alta producto",
                DatosAnteriores = JsonConvert.SerializeObject(new { Activo = false }),
                DatosNuevos = JsonConvert.SerializeObject(new { Activo = true }),
                Fecha = DateTime.Now
            };
            await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
        } 
        else 
        { 
            throw new Exception("No se pudo dar de alta el producto."); 
        }
    }

    public async Task EditarProductoAsync(int productoId, Producto productoEditado, IFormFile ImagenArchivo)
    {
        var producto = await _context.Productos.FindAsync(productoId);
        if (producto == null)
            throw new Exception("Producto no encontrado.");

        // Guardar datos anteriores para auditoría
        var datosAnteriores = new Producto
        {
            Nombre = producto.Nombre,
            Descripcion = producto.Descripcion,
            Precio = producto.Precio,
            Stock = producto.Stock,
            CategoriaId = producto.CategoriaId
        };

        // Realizar los cambios
        if (ImagenArchivo != null && ImagenArchivo.Length > 0)
        {
            using var ms = new MemoryStream();
            await ImagenArchivo.CopyToAsync(ms);
            productoEditado.Imagen = ms.ToArray();
            producto.Imagen = productoEditado.Imagen;
        }
        producto.Nombre = productoEditado.Nombre;
        producto.Descripcion = productoEditado.Descripcion;
        producto.Precio = productoEditado.Precio;
        producto.Stock = productoEditado.Stock;
        producto.CategoriaId = productoEditado.CategoriaId;

        int cambios = await _context.SaveChangesAsync();
        if (cambios > 0)
        {
            var auditoria = new Auditoria
            {
                UsuarioId = usuarioActivoId,
                Accion = "Editar Producto",
                DatosAnteriores = JsonConvert.SerializeObject(datosAnteriores),
                DatosNuevos = JsonConvert.SerializeObject(producto),
                Fecha = DateTime.Now
            };
            await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
        }
        else
        {
            throw new Exception("No se pudo editar el producto.");
        }
    }
}

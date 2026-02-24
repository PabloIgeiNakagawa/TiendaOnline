using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Productos
{
    public class ProductoCommandService : IProductoCommandService
    {
        private readonly TiendaContext _context;
        private readonly IImagenService _imagenService;

        public ProductoCommandService(TiendaContext context, IImagenService imagenService)
        {
            _context = context;
            _imagenService = imagenService;
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
    }
}
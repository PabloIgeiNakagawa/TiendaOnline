using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Productos
{
    public class ProductoCommandService : IProductoCommandService
    {
        private readonly TiendaContext _context;
        private readonly IImagenService _imagenService;

        // Tope de la galería (principal + extras): cada imagen consume capacidad
        // del plan de Cloudinary, igual que el clamp de dimensiones de subida.
        private const int MaxImagenesPorProducto = 6;

        public ProductoCommandService(TiendaContext context, IImagenService imagenService)
        {
            _context = context;
            _imagenService = imagenService;
        }

        public async Task AgregarProductoAsync(AgregarProductoDto dto, CancellationToken ct = default)
        {
            var extras = dto.ImagenesAdicionales ?? new List<ArchivoImagenDto>();

            if (extras.Count + 1 > MaxImagenesPorProducto)
                throw new ImagenNoValidaException($"El producto admite hasta {MaxImagenesPorProducto} imágenes (principal + adicionales).");

            string urlImagen = "/img/no-image.png";

            if (dto.ImagenStream != null)
            {
                urlImagen = await _imagenService.SubirImagenAsync(dto.ImagenStream, dto.NombreArchivo, "Productos", 800, 800, ct);
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

            // Extras en el orden de subida (Orden 1..n; la principal no vive en la
            // tabla). Si una subida explota, el producto nunca se guarda: quedan
            // huérfanas en Cloudinary, igual que si fallara la principal (comportamiento de siempre).
            var orden = 0;
            foreach (var extra in extras)
            {
                var url = await _imagenService.SubirImagenAsync(extra.Stream, extra.NombreArchivo, "Productos", 800, 800, ct);
                if (url != null)
                {
                    orden++;
                    nuevoProducto.Imagenes.Add(new ProductoImagen { Url = url, Orden = orden });
                }
            }

            _context.Productos.Add(nuevoProducto);
            await _context.SaveChangesAsync(ct);
        }

        public async Task EditarProductoAsync(EditarProductoDto dto, CancellationToken ct = default)
        {
            var producto = await _context.Productos
                .Include(p => p.Imagenes)
                .FirstOrDefaultAsync(p => p.ProductoId == dto.ProductoId, ct);
            if (producto == null) throw new KeyNotFoundException("Producto no encontrado.");

            if (dto.ImagenStream != null)
            {
                // Borrar la vieja si existe
                if (!string.IsNullOrEmpty(producto.ImagenUrl) && !producto.ImagenUrl.Contains("no-image.png"))
                {
                    string publicIdViejo = _imagenService.ExtraerPublicIdDesdeUrl(producto.ImagenUrl);
                    await _imagenService.BorrarImagenAsync(publicIdViejo, ct);
                }

                // Subir la nueva
                producto.ImagenUrl = await _imagenService.SubirImagenAsync(dto.ImagenStream, dto.NombreArchivo ?? "producto", "Productos", 800, 800, ct);
            }

            // Extras nuevas: se suman a la galería sin tocar las existentes.
            var extrasNuevas = dto.ImagenesAdicionalesNuevas ?? new List<ArchivoImagenDto>();
            if (extrasNuevas.Count + producto.Imagenes.Count + 1 > MaxImagenesPorProducto)
                throw new ImagenNoValidaException($"El producto admite hasta {MaxImagenesPorProducto} imágenes (principal + adicionales).");

            var orden = producto.Imagenes.Count;
            foreach (var extra in extrasNuevas)
            {
                var url = await _imagenService.SubirImagenAsync(extra.Stream, extra.NombreArchivo, "Productos", 800, 800, ct);
                if (url != null)
                {
                    orden++;
                    producto.Imagenes.Add(new ProductoImagen { Url = url, Orden = orden });
                }
            }

            // Actualización de campos
            producto.Nombre = dto.Nombre;
            producto.Descripcion = dto.Descripcion;
            producto.Precio = dto.Precio;
            producto.CategoriaId = dto.CategoriaId;

            await _context.SaveChangesAsync(ct);
        }

        public async Task EliminarImagenAsync(int productoId, int imagenId, CancellationToken ct = default)
        {
            // El par (producto, imagen) de la ruta es el único que autoriza: una
            // imagen de otro producto no se ve ni se borra por acá.
            var imagen = await _context.ImagenesProducto
                .FirstOrDefaultAsync(i => i.ProductoImagenId == imagenId && i.ProductoId == productoId, ct);

            if (imagen == null)
                throw new ImagenNoValidaException("La imagen no existe o no pertenece a este producto.");

            var publicId = _imagenService.ExtraerPublicIdDesdeUrl(imagen.Url);
            if (!string.IsNullOrEmpty(publicId))
                await _imagenService.BorrarImagenAsync(publicId, ct);

            _context.ImagenesProducto.Remove(imagen);
            await _context.SaveChangesAsync(ct);
        }

        public async Task DarAltaProductoAsync(int productoId, CancellationToken ct = default)
        {
            var producto = await _context.Productos.FindAsync(productoId, ct);
            if (producto == null) throw new Exception("Producto no encontrado.");

            var estadoAnterior = new { producto.ProductoId, producto.Nombre, producto.Activo };

            producto.Activo = true;

            await _context.SaveChangesAsync(ct);
        }

        public async Task DarBajaProductoAsync(int productoId, CancellationToken ct = default)
        {
            var producto = await _context.Productos.FindAsync(productoId, ct);
            if (producto == null) throw new Exception("Producto no encontrado.");

            var estadoAnterior = new { producto.ProductoId, producto.Nombre, producto.Activo };

            producto.Activo = false;

            await _context.SaveChangesAsync(ct);
        }
    }
}
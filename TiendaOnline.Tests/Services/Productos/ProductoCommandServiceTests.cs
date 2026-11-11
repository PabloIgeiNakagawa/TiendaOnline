using Microsoft.EntityFrameworkCore;
using Moq;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Exceptions;
using TiendaOnline.Infrastructure.Persistence;
using TiendaOnline.Tests.TestSupport;

namespace TiendaOnline.Tests.Services.Productos
{
    /// <summary>
    /// La galería de imágenes adicionales del producto: la principal vive en
    /// Producto.ImagenUrl (la leen el resto de las vistas) y la tabla nueva solo
    /// guarda las extras, en el orden de subida, con tope de 6 por producto.
    /// </summary>
    public class ProductoCommandServiceTests
    {
        private static AgregarProductoDto DtoAlta(
            List<ArchivoImagenDto>? extras = null,
            bool conPrincipal = true)
            => new()
            {
                Nombre = "Producto Test",
                Descripcion = "Descripción",
                Precio = 100m,
                Stock = 3,
                CategoriaId = 1, // el guard de categoría hoja vive en el controller
                ImagenStream = conPrincipal ? Stream.Null : null,
                NombreArchivo = conPrincipal ? "principal.png" : null,
                ImagenesAdicionales = extras
            };

        private static ArchivoImagenDto Extra(string nombre) => new()
        {
            Stream = Stream.Null,
            NombreArchivo = nombre
        };

        private static Mock<IImagenService> ImagenQueSube(params string[] urls)
        {
            var mock = new Mock<IImagenService>();
            var i = 0;
            mock.Setup(x => x.SubirImagenAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => urls[Math.Min(i++, urls.Length - 1)]);
            return mock;
        }

        [Fact]
        public async Task Agregar_ConExtras_CreaFilasOrdenadas()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var categoria = TestDatabase.SeedCategoria(ctx);
            var imagen = ImagenQueSube("https://img.test/principal.png", "https://img.test/extra-1.png", "https://img.test/extra-2.png");
            var dto = DtoAlta([Extra("a.png"), Extra("b.png")]);
            dto.CategoriaId = categoria.CategoriaId;
            var sut = Sut.ProductoCommand(ctx, imagen);

            await sut.AgregarProductoAsync(dto);

            var producto = await ctx.Productos.Include(p => p.Imagenes).SingleAsync();
            Assert.Equal("https://img.test/principal.png", producto.ImagenUrl);
            Assert.Equal(2, producto.Imagenes.Count);
            Assert.Equal([("https://img.test/extra-1.png", 1), ("https://img.test/extra-2.png", 2)],
                producto.Imagenes.OrderBy(i => i.Orden).Select(i => (i.Url, i.Orden)).ToList());
        }

        [Fact]
        public async Task Agregar_SinExtras_QuedaSoloLaPrincipal()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var categoria = TestDatabase.SeedCategoria(ctx);
            var dto = DtoAlta();
            dto.CategoriaId = categoria.CategoriaId;
            var sut = Sut.ProductoCommand(ctx, ImagenQueSube("https://img.test/principal.png"));

            await sut.AgregarProductoAsync(dto);

            var producto = await ctx.Productos.Include(p => p.Imagenes).SingleAsync();
            Assert.Empty(producto.Imagenes);
            Assert.Equal("https://img.test/principal.png", producto.ImagenUrl);
        }

        [Fact]
        public async Task Agregar_ExcedeElTope_LanzaYNoSubeNada()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var categoria = TestDatabase.SeedCategoria(ctx);
            // 6 extras + 1 principal = 7 > tope 6
            var dto = DtoAlta(Enumerable.Range(0, 6).Select(i => Extra($"{i}.png")).ToList());
            dto.CategoriaId = categoria.CategoriaId;
            var imagen = ImagenQueSube("https://img.test/x.png");
            var sut = Sut.ProductoCommand(ctx, imagen);

            await Assert.ThrowsAsync<ImagenNoValidaException>(() => sut.AgregarProductoAsync(dto));

            imagen.Verify(x => x.SubirImagenAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Empty(ctx.Productos);
        }

        [Fact]
        public async Task Editar_SumaExtras_SinTocarLasExistentes()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx);
            producto.Imagenes.Add(new ProductoImagen { Url = "https://img.test/vieja.png", Orden = 1 });
            ctx.SaveChanges();
            var dto = new EditarProductoDto
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                CategoriaId = producto.CategoriaId,
                ImagenesAdicionalesNuevas = [Extra("nueva.png")]
            };
            var sut = Sut.ProductoCommand(ctx, ImagenQueSube("https://img.test/nueva.png"));

            await sut.EditarProductoAsync(dto);

            var despues = await ctx.Productos.Include(p => p.Imagenes).SingleAsync(p => p.ProductoId == producto.ProductoId);
            Assert.Equal("https://img.test/producto.png", despues.ImagenUrl); // la principal no cambia
            Assert.Equal([("https://img.test/vieja.png", 1), ("https://img.test/nueva.png", 2)],
                despues.Imagenes.OrderBy(i => i.Orden).Select(i => (i.Url, i.Orden)).ToList());
        }

        [Fact]
        public async Task Editar_CambiaPrincipal_MantieneLaGaleria()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx);
            producto.Imagenes.Add(new ProductoImagen { Url = "https://img.test/extra.png", Orden = 1 });
            ctx.SaveChanges();
            var dto = new EditarProductoDto
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                CategoriaId = producto.CategoriaId,
                ImagenStream = Stream.Null,
                NombreArchivo = "nueva-principal.png"
            };
            var imagen = ImagenQueSube("https://img.test/principal-nueva.png");
            // La extracción del public id es del servicio real de Cloudinary: el
            // mock la fija para verificar que la baja llega con ese id.
            imagen.Setup(x => x.ExtraerPublicIdDesdeUrl("https://img.test/producto.png")).Returns("producto");
            var sut = Sut.ProductoCommand(ctx, imagen);

            await sut.EditarProductoAsync(dto);

            var despues = await ctx.Productos.Include(p => p.Imagenes).SingleAsync(p => p.ProductoId == producto.ProductoId);
            Assert.Equal("https://img.test/principal-nueva.png", despues.ImagenUrl);
            imagen.Verify(x => x.BorrarImagenAsync("producto", It.IsAny<CancellationToken>()), Times.Once);
            Assert.Single(despues.Imagenes);
            Assert.Equal("https://img.test/extra.png", despues.Imagenes.Single().Url);
        }

        [Fact]
        public async Task Editar_ExcedeElTope_SumandoExtras_Lanza()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx);
            for (var i = 1; i <= 5; i++)
                producto.Imagenes.Add(new ProductoImagen { Url = $"https://img.test/extra-{i}.png", Orden = i });
            ctx.SaveChanges();
            var dto = new EditarProductoDto
            {
                ProductoId = producto.ProductoId,
                Nombre = producto.Nombre,
                Descripcion = producto.Descripcion,
                Precio = producto.Precio,
                CategoriaId = producto.CategoriaId,
                ImagenesAdicionalesNuevas = [Extra("una-mas.png")]
            };
            var imagen = ImagenQueSube("https://img.test/x.png");
            var sut = Sut.ProductoCommand(ctx, imagen);

            await Assert.ThrowsAsync<ImagenNoValidaException>(() => sut.EditarProductoAsync(dto));

            Assert.Equal(5, await ctx.ImagenesProducto.CountAsync(i => i.ProductoId == producto.ProductoId));
        }

        [Fact]
        public async Task EliminarImagen_DeTuProducto_BajaDeCloudinaryYFila()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx);
            var fila = new ProductoImagen { ProductoId = producto.ProductoId, Url = "https://img.test/extra.png", Orden = 1 };
            ctx.ImagenesProducto.Add(fila);
            ctx.SaveChanges();
            var imagen = new Mock<IImagenService>();
            imagen.Setup(x => x.ExtraerPublicIdDesdeUrl("https://img.test/extra.png")).Returns("id-extra");
            var sut = Sut.ProductoCommand(ctx, imagen);

            await sut.EliminarImagenAsync(producto.ProductoId, fila.ProductoImagenId);

            imagen.Verify(x => x.BorrarImagenAsync("id-extra", It.IsAny<CancellationToken>()), Times.Once);
            Assert.Empty(await ctx.ImagenesProducto.Where(i => i.ProductoId == producto.ProductoId).ToListAsync());
        }

        [Fact]
        public async Task EliminarImagen_DeOtroProducto_LanzaYFilaIntacta()
        {
            using var db = new TestDatabase();
            await using var ctx = db.CreateContext();
            var producto = TestDatabase.SeedProducto(ctx);
            var fila = new ProductoImagen { ProductoId = producto.ProductoId, Url = "https://img.test/extra.png", Orden = 1 };
            ctx.ImagenesProducto.Add(fila);
            ctx.SaveChanges();
            var imagen = new Mock<IImagenService>();
            var sut = Sut.ProductoCommand(ctx, imagen);

            // id de producto que no es el dueño de la imagen
            await Assert.ThrowsAsync<ImagenNoValidaException>(() => sut.EliminarImagenAsync(producto.ProductoId + 99, fila.ProductoImagenId));

            imagen.Verify(x => x.BorrarImagenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Assert.Single(ctx.ImagenesProducto.Where(i => i.ProductoImagenId == fila.ProductoImagenId));
        }
    }
}

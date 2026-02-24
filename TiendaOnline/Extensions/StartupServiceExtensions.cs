using Microsoft.AspNetCore.Identity;
using TiendaOnline.Application.Auditoria;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Features.Admin.Categorias;
using TiendaOnline.Features.Admin.HomeAdmin;
using TiendaOnline.Features.Admin.MovimientosStock;
using TiendaOnline.Features.Admin.Pedidos;
using TiendaOnline.Features.Admin.Productos;
using TiendaOnline.Features.Admin.Reportes;
using TiendaOnline.Features.Admin.Usuarios;
using TiendaOnline.Features.Tienda.Account;
using TiendaOnline.Features.Tienda.Pedidos;
using TiendaOnline.Features.Tienda.Usuarios;
using TiendaOnline.Infrastructure.ExternalServices;
using TiendaOnline.Infrastructure.Services.Auditoria;
using TiendaOnline.Infrastructure.Services.Productos;

namespace TiendaOnline.Extensions
{
    public static class StartupServiceExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Servicios base
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IProductoQueryService, ProductoQueryService>();
            services.AddScoped<IProductoCommandService, ProductoCommandService>();
            services.AddScoped<ICategoriaService, CategoriaService>();
            services.AddScoped<IPedidoService, PedidoService>();

            // Administración
            services.AddScoped<IHomeAdminService, HomeAdminService>();
            services.AddScoped<IReportesService, ReportesService>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IMovimientoStockService, MovimientoStockService>();

            services.AddScoped<IUsuariosAdminService, UsuariosAdminService>();
            services.AddScoped<IPedidosAdminService, PedidosAdminService>();

            // Autenticación y Sesión de Usuario
            services.AddScoped<IAuthService, AuthService>();
            // Servicio para manejar la sesión del usuario
            services.AddScoped<IUserSessionService, UserSessionService>();
            // El servicio de Cloudinary (Interfaz en Domain, Implementación en Infrastructure)
            services.AddScoped<IImagenService, CloudinaryService>();

            // PasswordHasher (Usando la entidad del Domain)
            services.AddSingleton<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

            return services;
        }
    }
}

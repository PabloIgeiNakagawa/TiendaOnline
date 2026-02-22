using Microsoft.AspNetCore.Identity;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Features.Admin.Auditorias;
using TiendaOnline.Features.Admin.Categorias;
using TiendaOnline.Features.Admin.HomeAdmin;
using TiendaOnline.Features.Admin.MovimientosStock;
using TiendaOnline.Features.Admin.Reportes;
using TiendaOnline.Features.Tienda.Account;
using TiendaOnline.Features.Tienda.Pedidos;
using TiendaOnline.Infrastructure.ExternalServices;
using TiendaOnline.Services.IServices;
using TiendaOnline.Services.Services;

namespace TiendaOnline.Extensions
{
    public static class StartupServiceExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Servicios base
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IProductoService, ProductoService>();
            services.AddScoped<ICategoriaService, CategoriaService>();
            services.AddScoped<IPedidoService, PedidoService>();

            // Administración
            services.AddScoped<IHomeAdminService, HomeAdminService>();
            services.AddScoped<IReportesService, ReportesService>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IMovimientoStockService, MovimientoStockService>();

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

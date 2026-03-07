using Microsoft.AspNetCore.Identity;
using TiendaOnline.Application.AdminOverview;
using TiendaOnline.Application.Auditoria;
using TiendaOnline.Application.Auth;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Categorias.Commands;
using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Geo;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.MovimientosStock.Queries;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Application.Pedidos.Query;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Application.Reportes;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.ExternalServices;
using TiendaOnline.Infrastructure.Services.AdminOverview;
using TiendaOnline.Infrastructure.Services.Auditoria;
using TiendaOnline.Infrastructure.Services.Auth;
using TiendaOnline.Infrastructure.Services.Carritos;
using TiendaOnline.Infrastructure.Services.Categorias;
using TiendaOnline.Infrastructure.Services.Direcciones;
using TiendaOnline.Infrastructure.Services.Geo;
using TiendaOnline.Infrastructure.Services.MovimientosStock;
using TiendaOnline.Infrastructure.Services.Pedidos;
using TiendaOnline.Infrastructure.Services.Productos;
using TiendaOnline.Infrastructure.Services.Reportes;
using TiendaOnline.Infrastructure.Services.Usuarios;

namespace TiendaOnline.Extensions
{
    public static class StartupServiceExtensions
    {
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Servicios base
            services.AddScoped<IUsuarioQueryService, UsuarioQueryService>();
            services.AddScoped<IUsuarioCommandService, UsuarioCommandService>();
            services.AddScoped<IRolQueryService, RolQueryService>();
            services.AddScoped<IProductoQueryService, ProductoQueryService>();
            services.AddScoped<IProductoCommandService, ProductoCommandService>();
            services.AddScoped<ICategoriaQueryService, CategoriaQueryService>();
            services.AddScoped<ICategoriaCommandService, CategoriaCommandService>();
            services.AddScoped<ICarritoStorage, SessionCarritoStorage>();
            services.AddScoped<ICarritoService, CarritoService>();
            services.AddScoped<IPedidoQueryService, PedidoQueryService>();
            services.AddScoped<IPedidoCommandService, PedidoCommandService>();
            services.AddScoped<IGeoService, GeoService>();
            services.AddScoped<IDireccionService, DireccionService>();

            // Administración
            services.AddScoped<IAdminOverviewService, AdminOverviewService>();
            services.AddScoped<IReportesService, ReportesService>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IMovimientoStockCommandService, MovimientoStockCommandService>();
            services.AddScoped<IMovimientoStockQueryService, MovimientoStockQueryService>();

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

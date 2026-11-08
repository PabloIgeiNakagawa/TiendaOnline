using TiendaOnline.Application.Common.Settings;
using Hangfire;
using Microsoft.AspNetCore.Identity;
using QuestPDF.Infrastructure;
using TiendaOnline.Application.AdminOverview;
using TiendaOnline.Application.AppSettings;
using TiendaOnline.Application.Auditoria;
using TiendaOnline.Application.Auth;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.Categorias.Commands;
using TiendaOnline.Application.Categorias.Queries;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Direcciones;
using TiendaOnline.Application.Envios;
using TiendaOnline.Application.Geo;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.MovimientosStock.Queries;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Application.Pedidos.Queries;
using TiendaOnline.Application.Productos.Commands;
using TiendaOnline.Application.Productos.Queries;
using TiendaOnline.Application.Reportes;
using TiendaOnline.Application.Usuarios.Commands;
using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.ExternalServices;
using TiendaOnline.Infrastructure.Services.AdminOverview;
using TiendaOnline.Infrastructure.Services.AppSettings;
using TiendaOnline.Infrastructure.Services.Auditoria;
using TiendaOnline.Infrastructure.Services.Auth;
using TiendaOnline.Infrastructure.Services.Carritos;
using TiendaOnline.Infrastructure.Services.Categorias;
using TiendaOnline.Infrastructure.Services.ComprobantePdf;
using TiendaOnline.Infrastructure.Services.Direcciones;
using TiendaOnline.Infrastructure.Services.Email;
using TiendaOnline.Infrastructure.Services.Envios;
using TiendaOnline.Infrastructure.Services.Geo;
using Resend;
using TiendaOnline.Infrastructure.Services.MovimientosStock;
using TiendaOnline.Infrastructure.Services.Ofertas;
using TiendaOnline.Infrastructure.Services.Pedidos;
using TiendaOnline.Infrastructure.Services.Productos;
using TiendaOnline.Infrastructure.Services.Reportes;
using TiendaOnline.Infrastructure.Services.Usuarios;

namespace TiendaOnline.Extensions
{
    public static class StartupServiceExtensions
    {
        static StartupServiceExtensions()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public static IServiceCollection AddBusinessServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Mapear AppSettings a Clases (IOptions)
            services.Configure<ResendSettings>(configuration.GetSection("Resend"));
            services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
            services.Configure<MercadoPagoSettings>(configuration.GetSection("MercadoPago"));
            services.Configure<GoogleKeys>(configuration.GetSection("GoogleKeys"));
            services.Configure<GlobalSettings>(o => o.SiteUrl = configuration["SiteUrl"] ?? "");

            // Hangfire
            services.AddHangfire(config => config
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));

            services.AddHangfireServer();

            // Servicios base
            services.AddScoped<IUsuarioQueryService, UsuarioQueryService>();
            services.AddScoped<IUsuarioCommandService, UsuarioCommandService>();
            services.AddScoped<IRolQueryService, RolQueryService>();
            services.AddScoped<IProductoQueryService, ProductoQueryService>();
            services.AddScoped<IProductoCommandService, ProductoCommandService>();
            services.AddScoped<ICategoriaQueryService, CategoriaQueryService>();
            services.AddScoped<ICategoriaCommandService, CategoriaCommandService>();
            services.AddScoped<IOfertaQueryService, OfertaQueryService>();
            services.AddScoped<IOfertaCommandService, OfertaCommandService>();
            // El carrito reside según quién mira: logueado → BD (sobrevive la sesión),
            // anónimo → session. Al loguear, la fusión de sesión→BD la arma el migrador.
            services.AddScoped<SessionCarritoStorage>();
            services.AddScoped<DbCarritoStorage>();
            services.AddScoped<ICarritoStorage, RoutingCarritoStorage>();
            services.AddScoped<ICarritoSesionMigrador, CarritoSesionMigrador>();
            services.AddScoped<ICarritoService, CarritoService>();
            services.AddScoped<IPedidoQueryService, PedidoQueryService>();
            services.AddScoped<ICreacionPedidoService, CreacionPedidoService>();
            services.AddScoped<IConfirmacionPagoService, ConfirmacionPagoService>();
            services.AddScoped<IGestionTransferenciaService, GestionTransferenciaService>();
            services.AddScoped<IEstadosPedidoService, EstadosPedidoService>();
            services.AddScoped<IPedidoVencimientoService, PedidoVencimientoService>();
            services.AddScoped<IEnvioQueryService, EnvioQueryService>();
            services.AddScoped<IEnvioCommandService, EnvioCommandService>();
            services.AddScoped<IGeoService, GeoService>();
            services.AddScoped<IDireccionService, DireccionService>();
            services.AddScoped<IPaymentService, MercadoPagoService>();

            // Email
            services.AddOptions<ResendClientOptions>().Configure(o =>
            {
                o.ApiToken = configuration["Resend:ApiKey"] ?? "";
            });
            services.AddHttpClient<ResendClient>();
            services.AddTransient<IResend, ResendClient>();
            services.AddScoped<IEmailService, ResendEmailService>();

            // Administración
            services.AddScoped<IAdminOverviewService, AdminOverviewService>();
            services.AddScoped<IReportesService, ReportesService>();
            services.AddScoped<IAuditoriaService, AuditoriaService>();
            services.AddScoped<IMovimientoStockCommandService, MovimientoStockCommandService>();
            services.AddScoped<IMovimientoStockQueryService, MovimientoStockQueryService>();
            services.AddScoped<IAppSettingsService, AppSettingsService>();
            services.AddScoped<IComprobantePdfService, ComprobantePdfService>();

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

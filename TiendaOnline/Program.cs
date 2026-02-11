using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Services.IServices;
using TiendaOnline.Services.Services;
using TiendaOnline.Infrastructure.ExternalServices;
using TiendaOnline.Services.IServices.Admin;
using TiendaOnline.Services.Services.Admin;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpContext
builder.Services.AddHttpContextAccessor();

// Base de datos
builder.Services.AddDbContext<TiendaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TiendaOnline.Data")
    ));

// Sesion
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cookies
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Usuario/Login";
        config.LogoutPath = "/Usuario/Logout";
        // Por seguridad
        config.Cookie.HttpOnly = true;
        #if DEBUG
            config.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        #else
            /* En producción, se recomienda usar Always para asegurar que las cookies solo se envíen a través de HTTPS. 
               Pero por el somee lo dejamos en SameAsRequest para evitar problemas. */ 
            config.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
        #endif
            config.Cookie.SameSite = SameSiteMode.Strict;
    });

// Registro de Servicios (Inyección de Dependencias)
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();

// Administración
builder.Services.AddScoped<IHomeService, HomeService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();

// Servicio de autenticación
builder.Services.AddScoped<IAuthService, AuthService>();

// Servicio para manejar la sesión del usuario
builder.Services.AddScoped<IUserSessionService, UserSessionService>();

// El servicio de Cloudinary (Interfaz en Domain, Implementación en Infrastructure)
builder.Services.AddScoped<IImagenService, CloudinaryService>();

// PasswordHasher (Usando la entidad del Domain)
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseSession(); // Para el carrito
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 1. Ruta para Áreas (Primero)
app.MapControllerRoute(
    name: "MyAreas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 2. Ruta por defecto (Segundo)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
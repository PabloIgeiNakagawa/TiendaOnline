using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;
using TiendaOnline.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// HttpContext
builder.Services.AddHttpContextAccessor();

// Base de datos
builder.Services.AddDbContext<TiendaContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Sesion
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // El carrito se borra tras 30 min de inactividad
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cookies
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Usuario/Login";
        config.LogoutPath = "/Usuario/Logout";

        // Configuraciones de seguridad para la cookie
        config.Cookie.HttpOnly = true; // Evita acceso desde JavaScript (más seguro)
        #if DEBUG
            config.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        #else
            config.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        #endif
        config.Cookie.SameSite = SameSiteMode.Strict; // Previene CSRF
    });

// Services
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IPedidoService, PedidoService>();
builder.Services.AddScoped<IReportesService, ReportesService>();
builder.Services.AddScoped<IAuditoriaService, AuditoriaService>();
builder.Services.AddScoped<IImagenService, CloudinaryService>();
builder.Services.AddScoped<PasswordHasher<Usuario>>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseSession(); // Para el carrito

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
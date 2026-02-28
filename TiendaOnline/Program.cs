using Microsoft.EntityFrameworkCore;
using TiendaOnline.Extensions;
using TiendaOnline.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Busca las vistas en Features
builder.Services.AddControllersWithViews().AddFeatureFolders();

// HttpContext  
builder.Services.AddHttpContextAccessor();

// Base de datos
builder.Services.AddDbContext<TiendaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TiendaOnline.Infrastructure")
    ));

// Extensiones personalizadas para organizar la configuración
builder.Services.AddCustomSecurity(); // Configura Cookies y Session
builder.Services.AddBusinessServices(); // Registra todos tus Services

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; " +
        "img-src 'self' https: data:; " +
        "script-src 'self' https:; " +
        "style-src 'self' https: 'unsafe-inline'; " +
        "font-src 'self' https: data:;";

    await next();
});
app.UseStaticFiles();

app.UseRouting();

app.UseSession(); // Para el carrito
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
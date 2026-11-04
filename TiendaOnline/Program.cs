using Microsoft.EntityFrameworkCore;
using TiendaOnline.Extensions;
using TiendaOnline.Hangfire;
using TiendaOnline.Infrastructure.Persistence;
using Hangfire;
using TiendaOnline.Application.Pedidos.Command;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Busca las vistas en Features
builder.Services.AddControllersWithViews().AddFeatureFolders();

// HttpContext  
builder.Services.AddHttpContextAccessor();
// REGISTRO PARA LA API DE PROVINCIAS
builder.Services.AddHttpClient();

// Base de datos
builder.Services.AddDbContext<TiendaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TiendaOnline.Infrastructure")
    ));

// Extensiones personalizadas para organizar la configuración
builder.Services.AddCustomSecurity(builder.Configuration); // Configura Cookies y Session
builder.Services.AddBusinessServices(builder.Configuration); // Registra todos tus Services
builder.Services.AddMemoryCache();
builder.Services.AddRateLimitingConfig(); // Rate limiting para endpoints críticos

// Hash de contraseñas y protección de datos (para tokens, etc.)
// Llaves de DataProtection persistidas en App_Data del sitio: sobreviven recycles y deploys.
builder.Services.AddDataProtection()
    .SetApplicationName("TiendaOnline")
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection-Keys")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();

    // Aplica a la DB las migraciones pendientes antes de servir requests.
    // Si la DB no responde al arrancar, se loguea y la app queda arriba.
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<TiendaContext>();
            context.Database.Migrate();
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Migraciones pendientes no aplicadas al arrancar (DB no disponible). Se reintenta en el próximo boot.");
    }
}

// Seed idempotente de AppSettings: inserta solo las keys que faltan.
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<TiendaContext>();
        DbInitializer.SeedSettings(context);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Seed de AppSettings falló al arrancar (DB no disponible). Se reintenta en el próximo boot.");
}

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";

    context.Response.Headers.ContentSecurityPolicy =
        "default-src 'self'; " +
        "frame-src https://www.google.com https://maps.google.com;" +
        "img-src 'self' https: data:; " +
        "script-src 'self' https:; " +
        "style-src 'self' https: 'unsafe-inline'; " +
        "font-src 'self' https: data:;" +
        "connect-src 'self' https://apis.datos.gob.ar;";

    await next();
});
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter(); // Rate limiting antes de auth
app.UseSession(); // Para el carrito
app.UseAuthentication();
app.UseAuthorization();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthFilter() }
});

// Registro del job en el storage de Hangfire (SQL); si la DB no responde al
// arrancar, se loguea y se re-registra en el próximo boot (AddOrUpdate es idempotente).
try
{
    RecurringJob.AddOrUpdate<IPedidoVencimientoService>(
        "cancelar-pedidos-vencidos",
        x => x.CancelarPedidosVencidosAsync(),
        "*/5 * * * *");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "No se pudo registrar el job 'cancelar-pedidos-vencidos' (DB no disponible al arrancar). Se re-registra en el próximo boot.");
}

// 1. Ruta para áreas (Primero)
app.MapControllerRoute(
    name: "MyAreas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// 2. Ruta por defecto (Segundo)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

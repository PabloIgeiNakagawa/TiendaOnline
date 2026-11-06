using Microsoft.EntityFrameworkCore;
using TiendaOnline.Extensions;
using TiendaOnline.Filters;
using TiendaOnline.Hangfire;
using TiendaOnline.Infrastructure.Persistence;
using Hangfire;
using TiendaOnline.Application.Pedidos.Commands;
using Microsoft.AspNetCore.DataProtection;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Logging a archivo: la consola en shared hosting es un agujero negro.
// App_Data está bloqueada para HTTP: los logs solo se leen por FTP del sitio.
// Los niveles viven en código a propósito: el appsettings.Production.json del server no es controlado por el repo.
var carpetaLogs = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "logs");
Directory.CreateDirectory(carpetaLogs);

builder.Host.UseSerilog((context, loggerConfiguration) => loggerConfiguration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning) // apaga el ruido por-request de ASP.NET/EF (incluye SQL con parámetros)
                                                               // El webhook de MercadoPago registra la consulta cruda (payload, firma) solo en Debug:
                                                               // nunca datos sensibles de terceros en el archivo de producción.
    .MinimumLevel.Override("TiendaOnline.Features.Pagos.PagosWebhookController", LogEventLevel.Debug)
    .Enrich.FromLogContext()
    .WriteTo.File(
        Path.Combine(carpetaLogs, "app-.log"),
        rollingInterval: RollingInterval.Day,        // un archivo por día: app-20261104.log
        retainedFileCountLimit: 30,                  // ~1 mes de retención, limpieza automática
        fileSizeLimitBytes: 50_000_000,              // cap duro por archivo para no llenar la cuota del hosting
        rollOnFileSizeLimit: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}"));

// Busca las vistas en Features. El filtro global traduce las DomainException no
// manejadas a mensaje + redirect (ver Filters/DomainExceptionFilter).
builder.Services.AddControllersWithViews(o => o.Filters.Add<DomainExceptionFilter>()).AddFeatureFolders();

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

// Seed idempotente de AppSettings y de las zonas de envío (solo inserta lo que falta).
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<TiendaContext>();
        DbInitializer.SeedSettings(context);
        DbInitializer.SeedZonasEnvio(context);
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Seed de configuración inicial falló al arrancar (DB no disponible). Se reintenta en el próximo boot.");
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

// Access log: una línea por request con status y duración. Va después de UseStaticFiles
// para que los assets estáticos (css/js/img) no llenen el log.
app.UseSerilogRequestLogging();

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

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

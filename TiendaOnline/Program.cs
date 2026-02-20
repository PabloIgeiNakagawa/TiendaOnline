using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Busca las vistas en Features
builder.Services.AddControllersWithViews().AddFeatureFolders();

// HttpContext
builder.Services.AddHttpContextAccessor();

// Base de datos
builder.Services.AddDbContext<TiendaContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("TiendaOnline.Data")
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
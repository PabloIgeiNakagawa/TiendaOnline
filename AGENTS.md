# TiendaOnline

## Project Overview
- **Framework**: ASP.NET Core 8 con Clean Architecture
- **Database**: SQL Server + Entity Framework Core (Code First)
- **Frontend**: MVC con Razor Views + Bootstrap 5

## Project Structure
```
TiendaOnline/
├── TiendaOnline/                    # Web layer (MVC, Controllers, Views)
├── TiendaOnline.Application/         # Application layer (DTOs, Interfaces, Services)
├── TiendaOnline.Domain/            # Domain layer (Entities, Enums, Exceptions)
├── TiendaOnline.Infrastructure/    # Infrastructure (EF Core, Service implementations)
└── TiendaOnline.Tests/              # xUnit tests
```

## Database Schema
```
┌────────────────┐
│   Usuario      │ 1──┬──► Pedido
└────────────────┘    │    │
                       │    ├──► DetallePedido ──► Producto
                       │    │
                       │    └──► MovimientoStock
                       │
                       └──► Direccion

┌────────────────┐
│ MetodoDePago   │ 1──► Pedido

┌────────────────┐
│   Producto     │◄── Categoria (jerárquica)
│                └──► OfertaProducto
│                └──► MovimientoStock

┌────────────────┐
│   Auditoria    │  # Registra todos los cambios

┌────────────────┐
│  AppSetting    │  # Configuraciones key-value
└────────────────┘
```

## Commands
```bash
dotnet build                              # Build solution
dotnet test                              # Run all tests
dotnet test --filter "Test~Metodo"       # Run tests matching filter
dotnet run --project TiendaOnline        # Run app
dotnet ef migrations add <Name> -s TiendaOnline -p TiendaOnline.Infrastructure
dotnet ef database update -s TiendaOnline -p TiendaOnline.Infrastructure
```

## Code Conventions

### Naming
- Classes/Methods: `PascalCase` (español: `ObtenerPerfil`, `AgregarProducto`)
- Variables/Parameters: `camelCase`
- Private fields: `_camelCase`
- Interfaces: `I` prefix (`IUsuarioQueryService`)
- DTOs: sufijo `Dto` o `Request`

### Async
- Siempre usar sufijo `Async`: `ObtenerProductoAsync(int id)`
- Retornar `Task<T>` para valores, `Task` para void

### Dependency Injection
- Inyección por constructor
- Registrar en `Extensions/StartupServiceExtensions`

### Entity Framework
- Usar `AsNoTracking()` para queries de solo lectura
- Siempre `async/await` con métodos EF

### Error Handling
- Excepciones custom en `Domain/Exceptions`
- Controllers usan `try/catch` con `TempData["MensajeExito"]` / `TempData["MensajeError"]`

### Enums
- `Rol`: `Usuario`, `Administrador`, `Repartidor`
- `EstadoPago`: `Pendiente`, `Aprobado`, `Rechazado`, `Reembolsado`
- `EstadoPedido`: `Nuevo`, `EnPreparacion`, `Enviado`, `Entregado`, `Cancelado`
- `TipoMovimiento`: `EntradaStock`, `SalidaVenta`, `Devolucion`, `AjusteManual`, `CancelacionPedido`

## Arquitectura: Crear un nuevo CRUD

### 1. Crear/Editar DTOs en `Application/<Feature>/`
```csharp
// Commands (crear/actualizar)
public record CrearProductoDto(
    string Nombre,
    string Descripcion,
    decimal Precio,
    int Stock,
    int CategoriaId
);

// Queries (lectura)
public record ProductoDto(
    int ProductoId,
    string Nombre,
    decimal Precio,
    int Stock,
    string CategoriaNombre
);
```

### 2. Crear Interfaces en `Application/Common/Interfaces/`
```csharp
public interface IProductoQueryService
{
    Task<ProductoDto?> ObtenerProductoAsync(int id);
    Task<List<ProductoDto>> ObtenerTodosAsync();
}

public interface IProductoCommandService
{
    Task<int> CrearProductoAsync(CrearProductoDto dto);
    Task EditarProductoAsync(CrearProductoDto dto);
    Task EliminarProductoAsync(int id);
}
```

### 3. Implementar en `Infrastructure/Services/`
```csharp
public class ProductoQueryService : IProductoQueryService
{
    private readonly TiendaContext _context;

    public ProductoQueryService(TiendaContext context)
    {
        _context = context;
    }

    public async Task<ProductoDto?> ObtenerProductoAsync(int id)
    {
        return await _context.Productos
            .AsNoTracking()
            .Include(p => p.Categoria)
            .Where(p => p.ProductoId == id && p.Activo)
            .Select(p => new ProductoDto(...))
            .FirstOrDefaultAsync();
    }
}
```

### 4. Registrar en `Extensions/StartupServiceExtensions`
```csharp
services.AddScoped<IProductoQueryService, ProductoQueryService>();
services.AddScoped<IProductoCommandService, ProductoCommandService>();
```

## Workflows Comunes

### Agregar producto
1. `ProductoCommandService.CrearProductoAsync()` → Crea producto
2. Si es entrada inicial → `MovimientoStock` tipo `EntradaStock`

### Procesar pedido
1. Crear `Pedido` con estado `Nuevo`
2. Por cada item → crear `DetallePedido`
3. Actualizar stock → crear `MovimientoStock` tipo `SalidaVenta`
4. Al confirmar pago → estado `EnPreparacion`

### Cancelar pedido
1. Cambiar estado a `Cancelado`
2. Revertir stock → `MovimientoStock` tipo `CancelacionPedido`

## Rutas Principales
- Login: `/Accounts/Login`
- Productos: `/Productos`
- Pedidos: `/Pedidos`
- Admin: `/AdminUsuarios`, `/AdminPedidos`

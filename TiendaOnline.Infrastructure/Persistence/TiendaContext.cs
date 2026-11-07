using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using Newtonsoft.Json;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Application.Common.Interfaces;

namespace TiendaOnline.Infrastructure.Persistence
{
    public class TiendaContext : DbContext
    {
        private readonly IUserSessionService _userSession;

        public TiendaContext(DbContextOptions<TiendaContext> options, IUserSessionService userSession)
            : base(options)
        {
            _userSession = userSession;
        }

        // Columnas que jamás viajan al JSON de auditoría: un hash de contraseña no es un dato de negocio.
        private static readonly HashSet<string> PropiedadesExcluidas = new(StringComparer.Ordinal)
        {
            nameof(Usuario.Contrasena)
        };

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Captura ANTES del primer save: OriginalValues se pierden cuando la entidad pasa a Unchanged
            // y las entidades Added recién entonces reciben su id real.
            var auditoriasPendientes = CapturarAuditorias();
            if (auditoriasPendientes.Count == 0)
            {
                return await base.SaveChangesAsync(cancellationToken);
            }

            // Negocio y auditoría comparten commit: si la inserción de auditoría falla,
            // el cambio de negocio también se revierte (no quedan cambios sin registro).
            // Si el caller ya abrió transacción (pedidos, stock), la auditoría se suma a la misma.
            var transaccionPropia = Database.CurrentTransaction is null;
            var transaccion = transaccionPropia ? await Database.BeginTransactionAsync(cancellationToken) : null;

            try
            {
                var resultado = await base.SaveChangesAsync(cancellationToken);

                foreach (var (entry, auditoria) in auditoriasPendientes)
                {
                    // Solo las altas dependen del save: recién ahí la DB asigna el id real.
                    // Las bajas ya se completaron al capturar (tras el save su fila queda Detached).
                    if (auditoria.DatosNuevos is null)
                    {
                        auditoria.EntidadId = entry.Properties
                            .First(p => p.Metadata.IsPrimaryKey())
                            .CurrentValue?.ToString() ?? "0";
                        auditoria.DatosNuevos = Serializar(entry, valoresActuales: true, soloModificadas: false);
                    }

                    Auditorias.Add(auditoria);
                }

                await base.SaveChangesAsync(cancellationToken);

                if (transaccionPropia && transaccion != null)
                {
                    await transaccion.CommitAsync(cancellationToken);
                }

                return resultado;
            }
            catch
            {
                if (transaccionPropia && transaccion != null)
                {
                    await transaccion.RollbackAsync(cancellationToken);
                }
                throw;
            }
            finally
            {
                if (transaccion != null)
                {
                    await transaccion.DisposeAsync();
                }
            }
        }

        private List<(EntityEntry Entry, Auditoria Auditoria)> CapturarAuditorias()
        {
            var usuarioId = _userSession.GetUserId();
            var pendientes = new List<(EntityEntry, Auditoria)>();

            foreach (var entry in ChangeTracker.Entries())
            {
                // La propia tabla de auditoría no se audita (recursión infinita);
                // un Modified sin columnas marcadas es ruido de fix-up de navegaciones, no un cambio real.
                // El carrito del usuario se toca en cada interacción y refresh de vista
                // (agregar/quitar/refrescar precios): auditarlo es ruido sin valor de negocio.
                if (entry.Entity is Auditoria or CarritoUsuario ||
                    entry.State is EntityState.Detached or EntityState.Unchanged)
                    continue;
                if (entry.State == EntityState.Modified && !entry.Properties.Any(p => p.IsModified))
                    continue;

                var nombreTabla = entry.Entity.GetType().Name;
                var accionBase = entry.State switch
                {
                    EntityState.Added => $"Creó {nombreTabla}",
                    EntityState.Deleted => $"Eliminó {nombreTabla}",
                    _ => $"Actualizó {nombreTabla}"
                };

                var esAlta = entry.State == EntityState.Added;
                var esBaja = entry.State == EntityState.Deleted;
                // Solo los updates muestran delta de columnas; altas y bajas describen la fila entera.
                var soloModificadas = entry.State == EntityState.Modified;

                string? datosNuevos;
                string? entidadId;
                if (esAlta)
                {
                    // El id real lo asigna la DB en el save: DatosNuevos y EntidadId se completan después.
                    datosNuevos = null;
                    entidadId = null;
                }
                else
                {
                    datosNuevos = esBaja ? "{}" : Serializar(entry, valoresActuales: true, soloModificadas);
                    entidadId = entry.Properties
                        .First(p => p.Metadata.IsPrimaryKey())
                        .CurrentValue?.ToString() ?? "0";
                }

                pendientes.Add((entry, new Auditoria
                {
                    UsuarioId = usuarioId,
                    TablaAfectada = nombreTabla,
                    // Sin usuario de sesión (jobs de Hangfire, webhooks): la acción queda marcada Sistema.
                    Accion = usuarioId is null ? $"Sistema: {accionBase}" : accionBase,
                    Fecha = DateTime.Now,
                    DatosAnteriores = esAlta ? "{}" : Serializar(entry, valoresActuales: false, soloModificadas),
                    // En altas quedan null acá y se llenan después del primer save, cuando ya existe el id.
                    DatosNuevos = datosNuevos!,
                    EntidadId = entidadId!
                }));
            }

            return pendientes;
        }

        // Updates: solo las columnas que EF marcó como modificadas; altas y bajas: fila completa.
        private static string Serializar(EntityEntry entry, bool valoresActuales, bool soloModificadas)
        {
            var propiedades = new Dictionary<string, object?>(StringComparer.Ordinal);

            foreach (var propiedad in entry.Properties)
            {
                if (PropiedadesExcluidas.Contains(propiedad.Metadata.Name))
                    continue;
                if (soloModificadas && !propiedad.IsModified)
                    continue;

                propiedades[propiedad.Metadata.Name] = valoresActuales
                    ? propiedad.CurrentValue
                    : propiedad.OriginalValue;
            }

            return JsonConvert.SerializeObject(propiedades);
        }

        public DbSet<Auditoria> Auditorias { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Direccion> Direcciones { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<MovimientoStock> MovimientosStock { get; set; }
        public DbSet<OfertaProducto> OfertasProducto { get; set; }
        public DbSet<ZonaEnvio> ZonasEnvio { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }
        public DbSet<CarritoUsuario> CarritosUsuarios { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración de Dirección
            modelBuilder.Entity<Direccion>(entity =>
            {
                entity.HasOne(d => d.Usuario)
                      .WithMany(u => u.Direcciones)
                      .HasForeignKey(d => d.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade); // Si borro un usuario, mueren sus direcciones
            });

            // Índice único para el Email en la entidad Usuario
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Configuración para evitar ciclos de cascada en Categorías
            modelBuilder.Entity<Categoria>()
                .HasOne(c => c.CategoriaPadre)
                .WithMany(c => c.Subcategorias)
                .HasForeignKey(c => c.CategoriaPadreId)
                .OnDelete(DeleteBehavior.Restrict);

            // La FK Producto→Categoría es obligatoria y con Restrict: borrar físicamente
            // una categoría no puede arrastrar sus productos (la convención de EF la
            // dejaba en Cascade). Las bajas comerciales son lógicas (Activo/Activa);
            // Restrict queda como guard de última línea en la base.
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Categoria)
                .WithMany(c => c.Productos)
                .HasForeignKey(p => p.CategoriaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Precisión decimal del dinero: completa la config de tabla que en Domain
            // iba como atributo propio de EF. EF ya mapea decimal a (18,2) por default;
            // explicitarlo en un solo lugar (el contexto) deja al Domain sin dependencias.
            modelBuilder.Entity<Producto>()
                .Property(p => p.Precio)
                .HasPrecision(18, 2);

            modelBuilder.Entity<DetallePedido>()
                .Property(d => d.PrecioUnitario)
                .HasPrecision(18, 2);

            //  Configuración de MovimientoStock
            modelBuilder.Entity<MovimientoStock>(entity =>
            {
                // Guardar el Enum como String para que la DB sea legible (Desnormalización controlada)
                entity.Property(m => m.Tipo)
                    .HasConversion<string>()
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(m => m.Fecha)
                    .HasDefaultValueSql("GETDATE()"); // El servidor pone la fecha

                // Relación con Producto: Un producto tiene muchos movimientos
                entity.HasOne(m => m.Producto)
                    .WithMany(p => p.Movimientos)
                    .HasForeignKey(m => m.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict); // El historial de stock sobrevive a la baja del producto (trazabilidad, como DetallePedido.Producto)

                entity.HasOne(m => m.Pedido)
                    .WithMany(p => p.Movimientos)
                    .HasForeignKey(m => m.PedidoId)
                    .OnDelete(DeleteBehavior.SetNull); // Si se borra el pedido, el movimiento queda como historial

                // Índice para que los reportes de stock por producto vuelen
                entity.HasIndex(m => m.ProductoId);
                entity.HasIndex(m => m.Fecha);
            });

            // Configuración de Pedido
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.ToTable("Pedidos");

                // Relación con Usuario
                entity.HasOne(p => p.Usuario)
                      .WithMany(u => u.Pedidos)
                      .HasForeignKey(p => p.UsuarioId)
                      .OnDelete(DeleteBehavior.Restrict); // Si borro un usuario, NO se borran sus pedidos
            });

            // Configuración de DetallePedido
            modelBuilder.Entity<DetallePedido>(entity =>
            {
                entity.Property(dp => dp.PrecioUnitario)
                    .HasPrecision(18, 2);

                // Relación con Pedido
                entity.HasOne(dp => dp.Pedido)
                    .WithMany(p => p.DetallesPedido)
                    .HasForeignKey(dp => dp.PedidoId)
                    .OnDelete(DeleteBehavior.Cascade); // Si borras el pedido, se borran sus líneas

                // Relación con Producto
                entity.HasOne(dp => dp.Producto)
                    .WithMany()
                    .HasForeignKey(dp => dp.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict); // No se borra un producto que está en un pedido
            });

            // Configuración adicional para OfertaProducto
            modelBuilder.Entity<OfertaProducto>(entity =>
            {
                entity.ToTable("OfertaProducto");

                // Aseguramos la precisión decimal
                entity.Property(e => e.PrecioOferta)
                      .HasPrecision(18, 2);

                // Relación: Un producto puede tener muchas ofertas (historial)
                entity.HasOne(d => d.Producto)
                      .WithMany(p => p.Ofertas)
                      .HasForeignKey(d => d.ProductoId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configuración de ZonaEnvio: una tarifa por provincia (el seed la completa con las 24).
            modelBuilder.Entity<ZonaEnvio>(entity =>
            {
                entity.ToTable("ZonasEnvio");

                entity.Property(z => z.Costo)
                      .HasPrecision(18, 2);

                entity.HasIndex(z => z.Provincia)
                      .IsUnique();
            });

            // Carrito persistido: una fila por usuario (snapshot JSON como session).
            // El PK es el UsuarioId: no hay identidad porque el usuario ya existe.
            modelBuilder.Entity<CarritoUsuario>(entity =>
            {
                entity.ToTable("CarritosUsuarios");

                entity.Property(c => c.UsuarioId)
                      .ValueGeneratedNever();

                entity.HasOne<Usuario>()
                      .WithOne()
                      .HasForeignKey<CarritoUsuario>(c => c.UsuarioId)
                      .OnDelete(DeleteBehavior.Cascade); // Sin dueño, el carrito guardado sobra
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}

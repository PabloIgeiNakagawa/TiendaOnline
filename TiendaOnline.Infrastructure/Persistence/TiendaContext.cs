using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            int? currentUserId = _userSession.GetUserId();

            // Identificar las entradas para auditar (excluyendo la propia tabla Auditoria)
            var entradasParaAuditar = ChangeTracker.Entries()
                .Where(e => e.Entity is not Auditoria &&
                            e.State != EntityState.Detached &&
                            e.State != EntityState.Unchanged)
                .ToList();

            var listaTemporal = new List<(EntityEntry Entry, Auditoria Auditoria)>();

            foreach (var entry in entradasParaAuditar)
            {
                var nombreTabla = entry.Entity.GetType().Name;

                var auditoria = new Auditoria
                {
                    UsuarioId = currentUserId ?? -1,
                    TablaAfectada = nombreTabla,
                    Fecha = DateTime.Now,
                    Accion = entry.State switch
                    {
                        EntityState.Added => $"Creó {nombreTabla}",
                        EntityState.Modified => $"Actualizó {nombreTabla}",
                        EntityState.Deleted => $"Eliminó {nombreTabla}",
                        _ => entry.State.ToString()
                    },
                    // Capturamos valores anteriores antes de que se pierdan
                    DatosAnteriores = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                        ? JsonConvert.SerializeObject(entry.OriginalValues.ToObject())
                        : "{}"
                };

                listaTemporal.Add((entry, auditoria));
            }

            // Aquí se generan los IDs reales en la DB
            var resultado = await base.SaveChangesAsync(cancellationToken);

            // Ahora que tenemos IDs reales, completamos los campos
            if (listaTemporal.Any())
            {
                foreach (var item in listaTemporal)
                {
                    // Datos nuevos con el ID ya asignado por SQL
                    item.Auditoria.DatosNuevos = item.Entry.State != EntityState.Deleted
                        ? JsonConvert.SerializeObject(item.Entry.CurrentValues.ToObject())
                        : "{}";

                    // Obtener el ID de la entidad dinámicamente (sea CategoriaId, ProductoId, etc.)
                    var primaryKey = item.Entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
                    item.Auditoria.EntidadId = primaryKey?.CurrentValue?.ToString() ?? "0";

                    Auditorias.Add(item.Auditoria);
                }

                // Insertamos las filas de auditoría
                await base.SaveChangesAsync(cancellationToken);
            }

            return resultado;
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
                    .OnDelete(DeleteBehavior.Cascade);

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

            base.OnModelCreating(modelBuilder);
        }
    }
}

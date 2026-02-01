using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace TiendaOnline.Data
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
                    DatosAnteriores = (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
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
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<DetallePedido> DetallesPedido { get; set; }
        public DbSet<Categoria> Categorias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
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

            base.OnModelCreating(modelBuilder);
        }
    }
}

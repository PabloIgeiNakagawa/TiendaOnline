using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TiendaOnline.Domain.Interfaces;
using TiendaOnline.Domain.Entities;

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
            // 1. Obtener el ID del usuario actual desde los Claims (Auth)
            int? currentUserId = _userSession.GetUserId();

            // 2. Capturar los cambios antes de procesarlos
            var entradasAuditoria = new List<Auditoria>();

            foreach (var entry in ChangeTracker.Entries())
            {
                // Evitar auditar la propia tabla de auditoría para no crear un bucle infinito
                if (entry.Entity is Auditoria || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                    continue;

                var auditoria = new Auditoria
                {
                    UsuarioId = currentUserId ?? -1,
                    Accion = entry.State.ToString(),
                    Fecha = DateTime.Now,
                    // Serializamos solo las propiedades de la tabla (evita el error de referencia circular)
                    DatosAnteriores = entry.State == EntityState.Modified || entry.State == EntityState.Deleted
                        ? JsonConvert.SerializeObject(entry.OriginalValues.ToObject())
                        : "{}",
                    DatosNuevos = entry.State == EntityState.Added || entry.State == EntityState.Modified
                        ? JsonConvert.SerializeObject(entry.CurrentValues.ToObject())
                        : "{}"
                };

                entradasAuditoria.Add(auditoria);
            }

            // 3. Guardar los cambios principales
            var resultado = await base.SaveChangesAsync(cancellationToken);

            // 4. Si se guardaron cambios, insertar las auditorías
            if (entradasAuditoria.Any())
            {
                Auditorias.AddRange(entradasAuditoria);
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

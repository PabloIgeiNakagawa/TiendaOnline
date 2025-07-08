using Microsoft.EntityFrameworkCore;
using TiendaOnline.Models;

namespace TiendaOnline.Data
{
    public class TiendaContext : DbContext
    {
        public TiendaContext(DbContextOptions<TiendaContext> options) : base(options) { }

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

            base.OnModelCreating(modelBuilder);
        }
    }
}

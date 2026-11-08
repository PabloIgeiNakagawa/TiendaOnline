using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.E2E.Host
{
    /// <summary>
    /// Contexto de EF para el host E2E: igual que TiendaContext pero con un default
    /// de fecha compatible con SQLite (el GETDATE() original es T-SQL y rompe el
    /// CREATE TABLE del provider de SQLite; Fecha ya se setea en código siempre).
    /// </summary>
    public class E2ETiendaContext : TiendaContext
    {
        public E2ETiendaContext(DbContextOptions<TiendaContext> options, IUserSessionService userSession)
            : base(options, userSession)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MovimientoStock>()
                .Property(m => m.Fecha)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}

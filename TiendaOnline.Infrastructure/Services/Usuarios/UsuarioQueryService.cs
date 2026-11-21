using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Usuarios.Queries;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Usuarios
{
    public class UsuarioQueryService : IUsuarioQueryService
    {
        private readonly TiendaContext _context;

        public UsuarioQueryService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<UsuarioPerfilDto?> ObtenerPerfil(int usuarioId, CancellationToken ct = default)
        {
            var usuario = await _context.Usuarios
                .AsNoTracking()
                .Include(u => u.Direcciones)
                .FirstOrDefaultAsync(u => u.UsuarioId == usuarioId, ct);

            if (usuario == null) return null;

            // Una sola pasada por los pedidos del usuario; las dos estadísticas se
            // computan en memoria (mismo patrón que ReportesService con columnas de
            // texto, y los pedidos de un usuario son una lista chiquita).
            var pedidos = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.UsuarioId == usuarioId)
                .Select(p => new
                {
                    p.Estado,
                    p.EstadoPago,
                    TotalItems = p.DetallesPedido.Sum(d => (decimal?)(d.Cantidad * d.PrecioUnitario)),
                    p.CostoEnvio
                })
                .ToListAsync(ct);

            return new UsuarioPerfilDto
            {
                UsuarioId = usuario.UsuarioId,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Email = usuario.Email,
                Telefono = usuario.Telefono ?? string.Empty,
                Direccion = DireccionPrincipalLegible(usuario.Direcciones),
                FechaNacimiento = usuario.FechaNacimiento,
                FechaCreacion = usuario.FechaCreacion,
                Activo = usuario.Activo,
                UltimaFechaAlta = usuario.UltimaFechaAlta,
                UltimaFechaBaja = usuario.UltimaFechaBaja,
                CantidadPedidos = pedidos.Count(p => p.Estado != EstadoPedido.Cancelado),
                // Dinero con la regla de la casa (misma que ReportesService):
                // solo pago Aprobado suma — un pedido Pendiente/Rechazado nunca
                // entró y uno Reembolsado ya volvió. El envío también lo pagó
                // (el top clientes del dashboard lo suma igual).
                TotalGastado = pedidos
                    .Where(p => p.EstadoPago == EstadoPago.Aprobado)
                    .Sum(p => (p.TotalItems ?? 0m) + p.CostoEnvio)
            };
        }

        private static string DireccionPrincipalLegible(IEnumerable<Direccion> direcciones)
        {
            var principal = direcciones.FirstOrDefault(d => d.EsPrincipal);
            return principal != null ? $"{principal.Calle} {principal.Numero}, {principal.Localidad}" : "Sin dirección";
        }

        public async Task<UsuarioResumenDto?> ObtenerUsuarioAsync(int usuarioId, CancellationToken ct = default)
        {
            // Proyección mínima: los dos forms del perfil solo necesitan
            // identidad — la entidad completa (hash, fechas, estado) no cruza.
            return await _context.Usuarios
                .AsNoTracking()
                .Where(u => u.UsuarioId == usuarioId)
                .Select(u => new UsuarioResumenDto(u.UsuarioId, u.Nombre, u.Apellido, u.Telefono ?? string.Empty))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<PagedResult<UsuarioListadoDto>> ObtenerUsuariosPaginadosAsync(int pagina, int cantidad, string? buscar, string? rol, bool? activo, CancellationToken ct = default)
        {
            var query = _context.Usuarios.AsQueryable();

            if (!string.IsNullOrWhiteSpace(buscar))
                query = query.Where(u => u.Nombre.Contains(buscar) || u.Apellido.Contains(buscar) || u.Email.Contains(buscar));

            if (!string.IsNullOrEmpty(rol) && Enum.TryParse<Rol>(rol, out var rolEnum))
            {
                query = query.Where(u => u.Rol == rolEnum);
            }

            if (activo.HasValue)
                query = query.Where(u => u.Activo == activo.Value);

            var total = await query.CountAsync(ct);

            var items = await query
                .OrderBy(u => u.Apellido)
                .Skip((pagina - 1) * cantidad)
                .Take(cantidad)
                .Select(u => new UsuarioListadoDto
                {
                    UsuarioId = u.UsuarioId,
                    NombreCompleto = $"{u.Nombre} {u.Apellido}",
                    Email = u.Email,
                    RolNombre = u.Rol.ToString(),
                    Activo = u.Activo
                })
                .ToListAsync(ct);

            return new PagedResult<UsuarioListadoDto>(items, total, pagina, cantidad);
        }
    }
}

using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Common;
using TiendaOnline.Application.Pedidos.Queries;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class PedidoQueryService : IPedidoQueryService
    {
        private readonly TiendaContext _context;

        // Id de "Transferencia Bancaria" en la tabla MetodoDePago (ver migration MetodoDePagoYCambiosPedido)
        private const int MetodoPagoTransferenciaId = 2;

        public PedidoQueryService(TiendaContext context)
        {
            _context = context;
        }

        private static string? FormatearDireccionEnvio(TiendaOnline.Domain.Entities.Pedido p)
        {
            if (p.EsEnvioADomicilio == false) return null;

            return $"{p.EnvioCalle ?? ""} {p.EnvioNumero ?? ""}, Piso: {p.EnvioPiso ?? "N/A"}, Depto: {p.EnvioDepartamento ?? "N/A"}, {p.EnvioLocalidad ?? ""}, {p.EnvioProvincia ?? ""}";
        }

        public async Task<List<PedidoListadoUsuarioDto>> ObtenerPedidosDeUsuarioAsync(int id, CancellationToken ct)
        {
            // El SQL solo entrega las columnas de estado ya convertidas a texto
            // ('Enviado'); el cast al int del DTO ocurre en memoria después del
            // fetch: un cast SQL sobre la columna convertida entrega 0 o rompe.
            var filas = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.UsuarioId == id)
                .Select(p => new
                {
                    p.PedidoId,
                    p.FechaPedido,
                    p.FechaEnvio,
                    p.FechaEntrega,
                    p.FechaCancelado,
                    Estado = p.Estado,
                    EstadoPago = p.EstadoPago,

                    Productos = p.DetallesPedido
                                .Select(d => d.Producto.Nombre)
                                .ToList()
                })
                .ToListAsync(ct);

            return filas.Select(f => new PedidoListadoUsuarioDto
            {
                PedidoId = f.PedidoId,
                FechaPedido = f.FechaPedido,
                FechaEnvio = f.FechaEnvio,
                FechaEntrega = f.FechaEntrega,
                FechaCancelado = f.FechaCancelado,
                EstadoId = (int)f.Estado,
                EstadoPagoId = (int)f.EstadoPago,

                Productos = f.Productos
            }).ToList();
        }

        public async Task<PedidoDetallesDto?> ObtenerPedidoConDetallesAsync(int id, CancellationToken ct)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == id)
                .Select(p => new
                {
                    p.PedidoId,
                    p.FechaPedido,
                    p.FechaEnvio,
                    p.FechaEntrega,
                    p.FechaCancelado,
                    Estado = p.Estado,
                    EstadoPago = p.EstadoPago,
                    p.MetodoDePagoId,

                    UsuarioId = p.Usuario.UsuarioId,
                    UsuarioNombre = p.Usuario.Nombre,
                    UsuarioEmail = p.Usuario.Email,
                    UsuarioTelefono = p.Usuario.Telefono,

                    DireccionCompleta = FormatearDireccionEnvio(p),
                    Observaciones = p.EnvioObservaciones,


                    Items = p.DetallesPedido.Select(d => new
                    {
                        d.Producto.Nombre,
                        d.Producto.ImagenUrl,
                        d.Cantidad,
                        d.PrecioUnitario
                    }).ToList(),

                    p.TransaccionPagoId,
                    p.ComprobanteUrl,
                    p.FechaComprobante,
                    p.CostoEnvio
                })
                .FirstOrDefaultAsync(ct);

            if (pedido == null) return null;

            return new PedidoDetallesDto
            {
                PedidoId = pedido.PedidoId,
                FechaPedido = pedido.FechaPedido,
                FechaEnvio = pedido.FechaEnvio,
                FechaEntrega = pedido.FechaEntrega,
                FechaCancelado = pedido.FechaCancelado,
                EstadoId = (int)pedido.Estado,
                EstadoPagoId = (int)pedido.EstadoPago,
                MetodoPagoId = pedido.MetodoDePagoId,

                UsuarioId = pedido.UsuarioId,
                UsuarioNombre = pedido.UsuarioNombre,
                UsuarioEmail = pedido.UsuarioEmail,
                UsuarioTelefono = pedido.UsuarioTelefono,

                DireccionCompleta = pedido.DireccionCompleta,
                Observaciones = pedido.Observaciones,

                Items = pedido.Items.Select(i => new PedidoItemDto
                {
                    ProductoNombre = i.Nombre,
                    ProductoImagenUrl = i.ImagenUrl,
                    Cantidad = i.Cantidad,
                    PrecioUnitario = i.PrecioUnitario
                }).ToList(),

                TransaccionPagoId = pedido.TransaccionPagoId,
                ComprobanteUrl = pedido.ComprobanteUrl,
                FechaComprobante = pedido.FechaComprobante,
                CostoEnvio = pedido.CostoEnvio
            };
        }

        public async Task<ComprobantePedidoDto?> ObtenerComprobanteDtoAsync(int pedidoId, CancellationToken ct)
        {
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new
                {
                    p.PedidoId,
                    p.FechaPedido,
                    p.CostoEnvio,
                    UsuarioNombre = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                    UsuarioEmail = p.Usuario.Email,
                    UsuarioTelefono = p.Usuario.Telefono,
                    DireccionCompleta = FormatearDireccionEnvio(p),
                    MetodoPago = p.MetodoDePago.Nombre,
                    EstadoPago = p.EstadoPago,
                    p.TransaccionPagoId,
                    Items = p.DetallesPedido.Select(d => new
                    {
                        d.Producto.Nombre,
                        d.Cantidad,
                        d.PrecioUnitario,
                        Subtotal = d.Cantidad * d.PrecioUnitario
                    }).ToList(),
                    Subtotal = p.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario)
                })
                .FirstOrDefaultAsync(ct);

            if (pedido == null) return null;

            var total = pedido.Subtotal + pedido.CostoEnvio;
            var numeroComprobante = $"CI-0001-{pedido.PedidoId:D6}";

            return new ComprobantePedidoDto(
                pedido.PedidoId,
                numeroComprobante,
                DateTime.Now,
                pedido.UsuarioNombre,
                pedido.UsuarioEmail,
                pedido.UsuarioTelefono ?? "N/A",
                pedido.DireccionCompleta,
                pedido.MetodoPago,
                (int)pedido.EstadoPago,
                pedido.TransaccionPagoId,
                pedido.Items.Select(i => new ComprobanteItemDto(i.Nombre, i.Cantidad, i.PrecioUnitario, i.Subtotal)).ToList(),
                pedido.Subtotal,
                pedido.CostoEnvio,
                total
            );
        }

        public async Task<PagedResult<PedidoListadoDto>> ObtenerPedidosPaginadosAsync(PedidosFiltroDto filtros, CancellationToken ct)
        {
            var query = _context.Pedidos
                .AsNoTracking()
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(filtros.Busqueda))
            {
                query = query.Where(p => p.PedidoId.ToString().Contains(filtros.Busqueda) ||
                                         p.Usuario.Nombre.Contains(filtros.Busqueda) ||
                                         p.Usuario.Apellido.Contains(filtros.Busqueda));
            }

            // El valor del filtro nace como int y pasa a enum antes de comparar: la
            // comparación enum↔columna usa la conversión a texto del modelo; un cast
            // SQL sobre la columna ya convertida no devuelve nada (o rompe en SQL Server).
            if (filtros.EstadoId.HasValue)
            {
                var estadoFiltro = (Domain.Entities.EstadoPedido)filtros.EstadoId.Value;
                query = query.Where(p => p.Estado == estadoFiltro);
            }

            if (filtros.EstadoPagoId.HasValue)
            {
                var estadoPagoFiltro = (Domain.Entities.EstadoPago)filtros.EstadoPagoId.Value;
                query = query.Where(p => p.EstadoPago == estadoPagoFiltro);
            }
            if (filtros.Desde.HasValue) query = query.Where(p => p.FechaPedido >= filtros.Desde.Value);
            if (filtros.Hasta.HasValue) query = query.Where(p => p.FechaPedido <= filtros.Hasta.Value);

            // Filtro de Monto
            if (filtros.MontoMin.HasValue || filtros.MontoMax.HasValue)
            {
                // Proyectamos el total una sola vez para no repetir el cálculo del Sum
                var qMonto = query.Select(p => new
                {
                    p,
                    Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad) + p.CostoEnvio
                });

                if (filtros.MontoMin.HasValue)
                    qMonto = qMonto.Where(x => x.Total >= filtros.MontoMin.Value);

                if (filtros.MontoMax.HasValue)
                    qMonto = qMonto.Where(x => x.Total <= filtros.MontoMax.Value);

                query = qMonto.Select(x => x.p);
            }

            // Conteo Total
            var total = await query.CountAsync(ct);

            // Paginación y Mapeo a DTO
            // Estado/EstadoPago viajan como columnas ya convertidas a texto; el cast
            // al int del DTO ocurre en memoria tras el fetch (el CAST SQL sobre el
            // texto entregaba 0 en SQLite y rompería en SQL Server).
            var filas = await query
                .OrderByDescending(p => p.FechaPedido)
                .Skip((filtros.Pagina - 1) * filtros.Cantidad)
                .Take(filtros.Cantidad)
                .Select(p => new
                {
                    p.PedidoId,
                    NombreCliente = $"{p.Usuario.Nombre} {p.Usuario.Apellido}",
                    p.Usuario.Email,
                    p.FechaPedido,
                    p.FechaEntrega,
                    Estado = p.Estado,
                    EstadoPago = p.EstadoPago,
                    p.MetodoDePagoId,
                    TieneComprobante = p.ComprobanteUrl != null,
                    Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad) + p.CostoEnvio
                })
                .ToListAsync(ct);

            var items = filas.Select(f => new PedidoListadoDto
            {
                PedidoId = f.PedidoId,
                NombreCliente = f.NombreCliente,
                EmailCliente = f.Email,
                FechaPedido = f.FechaPedido,
                FechaEntrega = f.FechaEntrega,
                EstadoId = (int)f.Estado,
                EstadoPagoId = (int)f.EstadoPago,
                MetodoPagoId = f.MetodoDePagoId,
                TieneComprobante = f.TieneComprobante,
                Total = f.Total
            }).ToList();

            return new PagedResult<PedidoListadoDto>(items, total, filtros.Pagina, filtros.Cantidad);
        }

        // La cola del reparto: pedidos en camino (Enviado) e historial de las últimas
        // 24 h (Entregado). Columnas de lo que necesita la entrega — cliente,
        // contacto, dirección y observación; el resto del pedido es del dueño.
        public async Task<List<PedidoRepartoDto>> ObtenerPedidosEnviadosAsync(CancellationToken ct)
        {
            var pedidos = _context.Pedidos
                .AsNoTracking()
                .Where(p => p.Estado == Domain.Entities.EstadoPedido.Enviado)
                .OrderBy(p => p.FechaPedido); // los más viejos primero: primera salida de la cola

            return await FilaDeReparto(pedidos).ToListAsync(ct);
        }

        public async Task<List<PedidoRepartoDto>> ObtenerPedidosEntregadosUltimoDiaAsync(CancellationToken ct)
        {
            var desde = DateTime.Now.AddDays(-1);

            var pedidos = _context.Pedidos
                .AsNoTracking()
                .Where(p => p.Estado == Domain.Entities.EstadoPedido.Entregado && p.FechaEntrega >= desde)
                .OrderByDescending(p => p.FechaEntrega);

            return await FilaDeReparto(pedidos).ToListAsync(ct);
        }

        private IQueryable<PedidoRepartoDto> FilaDeReparto(IQueryable<Domain.Entities.Pedido> pedidos)
        {
            return pedidos.Select(p => new PedidoRepartoDto
            {
                PedidoId = p.PedidoId,
                FechaPedido = p.FechaPedido,
                FechaEntrega = p.FechaEntrega,
                NombreCliente = p.Usuario.Nombre + " " + p.Usuario.Apellido,
                Telefono = p.Usuario.Telefono,
                DireccionCompleta = FormatearDireccionEnvio(p),
                Observaciones = p.EnvioObservaciones,
                CantidadItems = p.DetallesPedido.Sum(d => d.Cantidad)
            });
        }

        public async Task<InstruccionesTransferenciaDto?> ObtenerInstruccionesTransferenciaAsync(int pedidoId, CancellationToken ct)
        {
            // AsNoTracking + anon-type: EstaVencido() es lógica en memoria, se evalúa acá abajo
            var pedido = await _context.Pedidos
                .AsNoTracking()
                .Where(p => p.PedidoId == pedidoId)
                .Select(p => new
                {
                    p.PedidoId,
                    p.FechaPedido,
                    p.Estado,
                    p.EstadoPago,
                    p.MetodoDePagoId,
                    p.ComprobanteUrl,
                    Total = p.DetallesPedido.Sum(d => d.PrecioUnitario * d.Cantidad) + p.CostoEnvio
                })
                .FirstOrDefaultAsync(ct);

            if (pedido == null) return null;

            var vencido = pedido.EstadoPago != Domain.Entities.EstadoPago.Aprobado
                && pedido.FechaPedido.Add(Domain.Entities.Pedido.TiempoMaximoPagoPendiente) <= DateTime.Now;

            return new InstruccionesTransferenciaDto
            {
                PedidoId = pedido.PedidoId,
                Total = pedido.Total,
                ComprobanteUrl = pedido.ComprobanteUrl,
                Vencido = vencido,
                PuedeSubirComprobante = pedido.Estado == Domain.Entities.EstadoPedido.Nuevo
                    && pedido.EstadoPago == Domain.Entities.EstadoPago.Pendiente
                    && pedido.MetodoDePagoId == MetodoPagoTransferenciaId
                    && pedido.ComprobanteUrl == null
                    && !vencido
            };
        }
    }
}

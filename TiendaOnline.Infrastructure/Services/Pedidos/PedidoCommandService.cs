using Microsoft.EntityFrameworkCore;
using TiendaOnline.Application.Carritos;
using TiendaOnline.Application.MovimientosStock.Commands;
using TiendaOnline.Application.Payment;
using TiendaOnline.Application.Pedidos.Command;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class PedidoCommandService : IPedidoCommandService
    {
        private readonly TiendaContext _context;
        private readonly IMovimientoStockCommandService _movimientoStockCommandService;

        public PedidoCommandService(TiendaContext context, IMovimientoStockCommandService movimientoStockCommandService)
        {
            _context = context;
            _movimientoStockCommandService = movimientoStockCommandService;
        }

        public async Task<PedidoPagoDto> CrearPedidoYPrepararPagoAsync(CrearPedidoDto dto)
        {
            if (dto.Items == null || !dto.Items.Any()) 
                throw new Exception("El pedido no tiene ítems.");

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var pedido = new Pedido
                {
                    UsuarioId = dto.UsuarioId,
                    MetodoDePagoId = dto.MetodoDePagoId,
                    EsEnvioADomicilio = dto.EsEnvioADomicilio,
                    EnvioCalle = dto.EnvioCalle,
                    EnvioNumero = dto.EnvioNumero,
                    EnvioPiso = dto.EnvioPiso,
                    EnvioDepartamento = dto.EnvioDepartamento,
                    EnvioObservaciones = dto.EnvioObservaciones,
                    EnvioLocalidad = dto.EnvioLocalidad,
                    EnvioProvincia = dto.EnvioProvincia,
                    EnvioCodigoPostal = dto.EnvioCodigoPostal,
                    FechaPedido = DateTime.Now,
                    Estado = EstadoPedido.Nuevo,
                    EstadoPago = EstadoPago.Pendiente,
                    DetallesPedido = new List<DetallePedido>()
                };

                foreach (var itemDto in dto.Items)
                {
                    // Buscamos el producto en BD para validar stock y obtener nombre real
                    var producto = await _context.Productos.FindAsync(itemDto.ProductoId);

                    if (producto == null) 
                        throw new Exception($"Producto {itemDto.ProductoId} no encontrado.");

                    if (producto.Stock < itemDto.Cantidad) 
                        throw new Exception($"Sin stock para {producto.Nombre}");

                    if (producto.Stock < itemDto.Cantidad)
                        throw new Exception($"Sin stock suficiente para {producto.Nombre}");

                    // 1. Descontamos stock en la entidad
                    producto.Stock -= itemDto.Cantidad;

                    pedido.DetallesPedido.Add(new DetallePedido
                    {
                        ProductoId = producto.ProductoId,
                        Cantidad = itemDto.Cantidad,
                        PrecioUnitario = producto.Precio,
                        Producto = producto // Importante para que el PaymentService tenga el nombre
                    });
                }

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // 2. Generamos movimientos de stock (solo lógica de auditoría)
                foreach (var detalle in pedido.DetallesPedido)
                {
                    _movimientoStockCommandService.GenerarMovimiento(
                        detalle.Producto,
                        -detalle.Cantidad,
                        TipoMovimiento.SalidaVenta,
                        pedido.PedidoId,
                        $"Reserva por Pedido #{pedido.PedidoId}"
                    );
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // RECARGAMOS EL PEDIDO CON SUS INCLUDES
                var pedidoCompleto = await _context.Pedidos
                    .Include(p => p.Usuario) // Traemos los datos del usuario (Email)
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto) // Traemos los datos del producto (Nombre)
                    .FirstOrDefaultAsync(p => p.PedidoId == pedido.PedidoId);

                if (pedidoCompleto == null) throw new Exception("Error al recuperar el pedido creado.");

                return new PedidoPagoDto
                {
                    PedidoId = pedidoCompleto.PedidoId,
                    EmailUsuario = pedidoCompleto.Usuario.Email,
                    Items = pedidoCompleto.DetallesPedido.Select(d => new ItemPagoDto
                    {
                        Nombre = d.Producto.Nombre,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario
                    }).ToList()
                };
            }
            catch (Exception)
            {
                //_logger.LogError(ex, "Error al crear el pedido para el usuario {UsuarioId}", usuarioId);
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ConfirmarPagoAsync(InfoPagoDto infoPago)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.DetallesPedido)
                .FirstOrDefaultAsync(p => p.PedidoId == infoPago.PedidoId);

            // Idempotencia y existencia: Si no existe o ya está pagado, cortamos acá
            if (pedido == null || pedido.EstadoPago == EstadoPago.Aprobado)
                return false;

            // Calculamos el total en el momento
            decimal totalCalculado = pedido.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);

            // Seguridad: Comparamos el monto pagado en MP vs el total en nuestra DB
            // Usamos una diferencia menor a 0.01 por temas de decimales en C#
            if (Math.Abs((double)infoPago.MontoPagado - (double)totalCalculado) > 0.01)
            {
                // Fraude o error de montos
                return false;
            }

            // Confirmamos el pago
            if (infoPago.Estado == "approved")
            {
                pedido.EstadoPago = EstadoPago.Aprobado;
                pedido.Estado = EstadoPedido.EnPreparacion;
                pedido.TransaccionPagoId = infoPago.TransaccionId;

                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<PedidoPagoDto?> ObtenerDatosParaPagoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.DetallesPedido)
                    .ThenInclude(d => d.Producto)
                .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

            if (pedido == null) return null;

            // Solo permitimos reintentar si el pedido sigue pendiente
            if (pedido.EstadoPago == EstadoPago.Aprobado)
                throw new Exception("Este pedido ya ha sido pagado.");

            return new PedidoPagoDto
            {
                PedidoId = pedido.PedidoId,
                EmailUsuario = pedido.Usuario.Email,
                Items = pedido.DetallesPedido.Select(d => new ItemPagoDto
                {
                    Nombre = d.Producto.Nombre,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                }).ToList()
            };
        }

        public async Task PedidoEnviadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            if (pedido.Estado != EstadoPedido.EnPreparacion)
                throw new Exception("El pedido no se puede enviar porque no está en estado Pendiente (Estado actual: " + pedido.Estado + ")");

            pedido.Estado = EstadoPedido.Enviado;
            pedido.FechaEnvio = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task PedidoEntregadoAsync(int pedidoId)
        {
            var pedido = await _context.Pedidos.FindAsync(pedidoId);
            if (pedido == null) throw new Exception("Pedido no encontrado.");

            if (pedido.Estado != EstadoPedido.Enviado)
                throw new Exception($"No se puede entregar: el pedido aún figura como {pedido.Estado}.");

            pedido.Estado = EstadoPedido.Entregado;
            pedido.FechaEntrega = DateTime.Now;

            await _context.SaveChangesAsync();
        }

        public async Task PedidoCanceladoAsync(int pedidoId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Traemos el pedido con sus detalles (Importante usar .Include)
                var pedido = await _context.Pedidos
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(dp => dp.Producto) // Traemos el producto para devolverle el stock
                    .FirstOrDefaultAsync(p => p.PedidoId == pedidoId);

                if (pedido == null) throw new Exception("Pedido no encontrado.");

                // Validaciones de negocio
                if (pedido.Estado == EstadoPedido.Cancelado)
                    throw new Exception("El pedido ya estaba cancelado.");

                if (pedido.Estado == EstadoPedido.Entregado)
                    throw new Exception("No se puede cancelar un pedido que ya ha sido entregado.");

                if (pedido.Estado == EstadoPedido.Enviado)
                    throw new Exception("El pedido ya fue enviado, debe procesarse como devolución.");

                // Devolver el stock por cada detalle
                foreach (var detalle in pedido.DetallesPedido)
                {
                    // Sumamos lo que antes restamos
                    detalle.Producto.Stock += detalle.Cantidad;

                    // Registramos el movimiento de entrada por cancelación
                    // Cantidad positiva porque entra de nuevo
                    _movimientoStockCommandService.GenerarMovimiento(
                        detalle.Producto,
                        detalle.Cantidad,
                        TipoMovimiento.CancelacionPedido,
                        pedido.PedidoId,
                        $"Devolución por cancelación de Pedido #{pedido.PedidoId}"
                    );
                }

                pedido.Estado = EstadoPedido.Cancelado;
                pedido.EstadoPago = EstadoPago.Rechazado;
                pedido.FechaCancelado = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}

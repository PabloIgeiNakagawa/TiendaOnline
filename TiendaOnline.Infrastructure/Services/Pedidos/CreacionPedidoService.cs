using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TiendaOnline.Application.Common.Interfaces;
using TiendaOnline.Application.Ofertas;
using TiendaOnline.Application.Pedidos.Commands;
using TiendaOnline.Application.Payment;
using TiendaOnline.Domain.Entities;
using TiendaOnline.Infrastructure.Persistence;

namespace TiendaOnline.Infrastructure.Services.Pedidos
{
    public class CreacionPedidoService : ICreacionPedidoService
    {
        private readonly TiendaContext _context;
        private readonly IOfertaQueryService _ofertaQueryService;
        private readonly ILogger<CreacionPedidoService> _logger;

        public CreacionPedidoService(TiendaContext context, IOfertaQueryService ofertaQueryService, ILogger<CreacionPedidoService> logger)
        {
            _context = context;
            _ofertaQueryService = ofertaQueryService;
            _logger = logger;
        }

        public async Task<PedidoPagoDto> CrearPedidoYPrepararPagoAsync(CrearPedidoDto dto, CancellationToken ct = default)
        {
            if (dto.Items == null || !dto.Items.Any())
                throw new Exception("El pedido no tiene ítems.");

            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                // CONSOLIDAMOS items por ProductoId para evitar duplicados
                var itemsAgrupados = dto.Items
                    .GroupBy(i => i.ProductoId)
                    .Select(g => new CrearPedidoDetalleDto
                    {
                        ProductoId = g.Key,
                        Cantidad = g.Sum(x => x.Cantidad),
                        PrecioUnitario = g.First().PrecioUnitario
                    })
                    .ToList();

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
                    CostoEnvio = dto.CostoEnvio,
                    FechaPedido = DateTime.Now,
                    Estado = EstadoPedido.Nuevo,
                    EstadoPago = EstadoPago.Pendiente,
                    DetallesPedido = new List<DetallePedido>()
                };

                foreach (var itemDto in itemsAgrupados)
                {
                    // Guardamos el snapshot del pedido, pero el stock se reservará recién cuando el pago sea aprobado.
                    var producto = await _context.Productos.FindAsync(itemDto.ProductoId, ct);
                    if (producto == null)
                        throw new Exception($"No se encontró el producto con ID {itemDto.ProductoId}.");

                    // Precio efectivo del cobro: misma regla que vio el cliente
                    // en carrito/checkout — mejor oferta vigente ?? precio de lista.
                    var oferta = await _ofertaQueryService.ObtenerOfertaVigenteAsync(producto.ProductoId, ct);

                    pedido.DetallesPedido.Add(new DetallePedido
                    {
                        ProductoId = producto.ProductoId,
                        Cantidad = itemDto.Cantidad,
                        PrecioUnitario = oferta?.PrecioOferta ?? producto.Precio,
                        Producto = producto // Importante para que el PaymentService tenga el nombre
                    });
                }

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                // RECARGAMOS EL PEDIDO CON SUS INCLUDES
                var pedidoCompleto = await _context.Pedidos
                    .Include(p => p.Usuario) // Traemos los datos del usuario (Email)
                    .Include(p => p.DetallesPedido)
                        .ThenInclude(d => d.Producto) // Traemos los datos del producto (Nombre)
                    .FirstOrDefaultAsync(p => p.PedidoId == pedido.PedidoId, ct);

                if (pedidoCompleto == null) throw new Exception("Error al recuperar el pedido creado.");

                // Enviar email de confirmación usando Hangfire (Persistente y con reintentos)
                var subtotal = pedidoCompleto.DetallesPedido.Sum(d => d.Cantidad * d.PrecioUnitario);
                var total = subtotal + pedidoCompleto.CostoEnvio;

                BackgroundJob.Enqueue<IEmailService>(x => x.EnviarConfirmacionPedidoAsync(
                    pedidoCompleto.Usuario.Email,
                    pedidoCompleto.Usuario.Nombre,
                    pedidoCompleto.PedidoId,
                    total
                ));

                return new PedidoPagoDto
                {
                    PedidoId = pedidoCompleto.PedidoId,
                    EmailUsuario = pedidoCompleto.Usuario.Email,
                    // La preferencia de MercadoPago suma el ítem "Envío" cuando
                    // CostoEnvio > 0: el cobro matchea el total (subtotal + envío)
                    // que valida ConfirmarPagoAsync.
                    CostoEnvio = pedidoCompleto.CostoEnvio,
                    Items = pedidoCompleto.DetallesPedido.Select(d => new ItemPagoDto
                    {
                        Nombre = d.Producto.Nombre,
                        Cantidad = d.Cantidad,
                        PrecioUnitario = d.PrecioUnitario
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear el pedido.");
                await TransaccionSegura.RevertirAsync(transaction, _logger, ct);
                throw;
            }
        }
    }
}

using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Features.Pedidos
{
    public class MisPedidosViewModel
    {
        public List<PedidoListaViewModel> Pedidos { get; set; } = new();

        // Filtro por estado (client-side): el chip activo y los conteos, que se
        // calculan SIEMPRE sobre la lista completa (no sobre la filtrada).
        public string? EstadoFiltro { get; set; }
        public int ConteoTodos { get; set; }
        public int ConteoEnCurso { get; set; }
        public int ConteoEntregados { get; set; }
        public int ConteoCancelados { get; set; }

        /// <summary>
        /// El ?estado= de la URL solo acepta los tres grupos de las tarjetas;
        /// cualquier otro valor (o "todos") deja la lista completa visible.
        /// </summary>
        public bool HayFiltroValido => EstadoFiltro is "enCurso" or "entregados" or "cancelados";
    }

    public class PedidoProductoListaViewModel
    {
        public string Nombre { get; set; } = "";
        public string ImagenUrl { get; set; } = "";
        public int Cantidad { get; set; }
    }

    public class PedidoListaViewModel
    {
        public int PedidoId { get; set; }
        public DateTime FechaPedido { get; set; }
        public DateTime? FechaEnvio { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaCancelado { get; set; }
        public decimal Total { get; set; }
        public int CantidadItems { get; set; }
        public List<PedidoProductoListaViewModel> Productos { get; set; } = new();

        public EstadoPedido Estado { get; set; }

        public EstadoPago EstadoPago { get; set; }

        public TiendaOnline.Enums.MetodoPagoId MetodoPagoId { get; set; }

        public string? ComprobanteUrl { get; set; }

        public bool EstaEnviado => FechaEnvio.HasValue;
        public bool EstaEntregado => FechaEntrega.HasValue;
        public bool EstaCancelado => FechaCancelado.HasValue;

        /// <summary>
        /// Grupo del filtro de estado que contiene al pedido: la tarjeta viaja
        /// con este valor en data-estado-grupo y el filtro client-side decide
        /// qué mostrar a partir de él (mismos tres grupos que los chips).
        /// </summary>
        public string GrupoEstado =>
            EstaCancelado ? "cancelados" :
            EstaEntregado ? "entregados" :
            "enCurso";

        /// <summary>
        /// Misma regla del guard de Instrucciones (PuedeSubirComprobante): el
        /// pedido transferencia que espera su comprobante ofrece el atajo
        /// directo a la subida desde la lista. La ventana de 24h la evalúa en
        /// memoria (el job de vencimiento corre cada 5 min).
        /// </summary>
        public bool FaltaComprobante =>
            MetodoPagoId == TiendaOnline.Enums.MetodoPagoId.Transferencia
            && Estado == EstadoPedido.Nuevo
            && EstadoPago == EstadoPago.Pendiente
            && ComprobanteUrl == null
            && !EstaVencido;

        /// <summary>
        /// Límite de la ventana de pago que muestra la tarjeta junto al botón
        /// de subir comprobante (el mismo que evalúa FaltaComprobante).
        /// </summary>
        public DateTime FechaLimiteComprobante => FechaPedido.Add(Pedido.TiempoMaximoPagoPendiente);

        private bool EstaVencido =>
            EstadoPago != EstadoPago.Aprobado
            && FechaPedido.Add(Pedido.TiempoMaximoPagoPendiente) <= DateTime.Now;
    }
}
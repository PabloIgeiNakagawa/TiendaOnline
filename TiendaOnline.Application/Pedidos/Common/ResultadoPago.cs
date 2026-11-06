namespace TiendaOnline.Application.Pedidos.Common
{
    /// <summary>
    /// Motivo del resultado de procesar un pago externo (webhook MP o retorno del comprador).
    /// Cada valor explica por qué el pedido arranca en ese estado: los.logs y los mensajes
    /// del front nacen de acá, no de un bool indistinguible.
    /// </summary>
    public enum MotivoPago
    {
        /// <summary>La señal de MP aplicó una transición (aprobado / rechazado / cancelado / reembolsado).</summary>
        Confirmado,

        /// <summary>Retry benigno: el pago ya estaba confirmado, nada cambió.</summary>
        YaConfirmado,

        /// <summary>No existe pedido con el id externo.</summary>
        PedidoNoEncontrado,

        /// <summary>El monto reportado no coincide con el calculado (sospecha, requiere revisión humana).</summary>
        MontoNoCoincide,

        /// <summary>No se pudo reservar stock: el pago llegó pero ya no hay unidades.</summary>
        StockInsuficiente,

        /// <summary>Aprobación fuera del plazo: el pedido ya venció.</summary>
        PedidoVencido,

        /// <summary>La señal contradictoria con el estado actual del pedido: se ignora (ya cancelado / reembolsado / etc.).</summary>
        EstadoIncoherente,

        /// <summary>Estado de MP sin accion asociada en el sistema (p. ej. in_process / desconocido).</summary>
        EstadoNoManejado
    }

    /// <summary>
    /// Resultado del procesamiento del pago: boolean de confirmación + motivo tipado.
    /// Confirmado/YaConfirmado son los únicos casos exitosos (el cliente puede avanzar y vaciar el carrito).
    /// </summary>
    public record ResultadoPago(bool Confirmado, MotivoPago Motivo)
    {
        public static ResultadoPago Exito() => new(true, MotivoPago.Confirmado);
        public static ResultadoPago ExitoYaConfirmado() => new(true, MotivoPago.YaConfirmado);
        public static ResultadoPago Fallo(MotivoPago motivo) => new(false, motivo);
    }
}

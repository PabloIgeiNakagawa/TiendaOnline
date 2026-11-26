using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public enum EstadoPago
    {
        [Display(Name = "Pendiente")]
        Pendiente = 0,

        [Display(Name = "Aprobado")]
        Aprobado = 1,

        [Display(Name = "Rechazado")]
        Rechazado = 2,

        [Display(Name = "Reembolsado")]
        Reembolsado = 3
    }

    public enum EstadoPedido
    {
        [Display(Name = "Nuevo")]
        Nuevo = 0,

        [Display(Name = "En preparación")]
        EnPreparacion = 1, // Ya se pagó, el admin está armando la caja

        [Display(Name = "Enviado")]
        Enviado = 2,

        [Display(Name = "Entregado")]
        Entregado = 3,

        [Display(Name = "Cancelado")]
        Cancelado = 4
    }

    public class Pedido
    {
        public static readonly TimeSpan TiempoMaximoPagoPendiente = TimeSpan.FromHours(24);

        [Key]
        public int PedidoId { get; set; }

        [Required]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        public DateTime? FechaEnvio { get; set; }

        public DateTime? FechaEnPreparacion { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public DateTime? FechaCancelado { get; set; }

        // --- Manejo de Estados Separados ---
        [Required]
        public EstadoPedido Estado { get; set; } = EstadoPedido.Nuevo;

        [Required]
        public EstadoPago EstadoPago { get; set; } = EstadoPago.Pendiente;

        // --- Pagos ---
        // El método de pago es el enum MetodoPago: única fuente de verdad, la
        // base lo guarda como texto (misma regla que Estado/EstadoPago).
        [Required]
        public MetodoPago MetodoPago { get; set; } = MetodoPago.MercadoPago;

        [MaxLength(100)]
        public string? TransaccionPagoId { get; set; } // Acá guardás el ID que te devuelve MP o Stripe

        // --- Comprobante de pago por transferencia ---
        [MaxLength(500)]
        public string? ComprobanteUrl { get; set; }

        public DateTime? FechaComprobante { get; set; }

        [Required]
        public bool EsEnvioADomicilio { get; set; }

        [MaxLength(100)]
        public string? EnvioCalle { get; set; }

        [MaxLength(10)]
        public string? EnvioNumero { get; set; }

        [MaxLength(10)]
        public string? EnvioPiso { get; set; }

        [MaxLength(10)]
        public string? EnvioDepartamento { get; set; }

        [MaxLength(250)]
        public string? EnvioObservaciones { get; set; }

        [MaxLength(100)]
        public string? EnvioLocalidad { get; set; }

        [MaxLength(100)]
        public string? EnvioProvincia { get; set; }

        [MaxLength(15)]
        public string? EnvioCodigoPostal { get; set; }

        public decimal CostoEnvio { get; set; }

        // ---- Relaciones ----
        [Required]
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        // Quién lleva el pedido: null hasta que el repartidor lo toma o el admin
        // lo asigna. La entrega y el seguimiento en vivo se apoyan en esta fila.
        public int? RepartidorId { get; set; }
        public Usuario? Repartidor { get; set; }

        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
        public virtual ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();

        public bool EstaVencido()
        {
            if (EstadoPago == EstadoPago.Aprobado)
                return false;

            return FechaPedido.Add(TiempoMaximoPagoPendiente) <= DateTime.Now;
        }
    }
}

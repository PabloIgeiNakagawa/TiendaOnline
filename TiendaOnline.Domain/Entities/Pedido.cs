using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Required]
        public int MetodoDePagoId { get; set; }
        public MetodoDePago MetodoDePago { get; set; } // Propiedad de navegación

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

        [Column(TypeName = "decimal(18,2)")]
        public decimal CostoEnvio { get; set; }

        // ---- Relaciones ----
        [Required]
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

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

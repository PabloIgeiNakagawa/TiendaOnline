using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public enum EstadoPago
    {
        Pendiente = 0,
        Aprobado = 1,
        Rechazado = 2,
        Reembolsado = 3
    }

    public enum EstadoPedido
    {
        Nuevo = 0,
        EnPreparacion = 1, // Ya se pagó, el admin está armando la caja
        Enviado = 2,
        Entregado = 3,
        Cancelado = 4
    }

    public class Pedido
    {
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

        // ---- Relaciones ----
        [Required]
        public int UsuarioId { get; set; }
        public Usuario Usuario { get; set; }

        public ICollection<DetallePedido> DetallesPedido { get; set; } = new List<DetallePedido>();
        public virtual ICollection<MovimientoStock> Movimientos { get; set; } = new List<MovimientoStock>();
    }
}

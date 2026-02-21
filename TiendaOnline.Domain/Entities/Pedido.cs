using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Domain.Entities
{
    public enum EstadoPedido
    {
        Pendiente = 0,
        Enviado = 1,
        Entregado = 2,
        Cancelado = 3
    }

    public class Pedido
    {
        [Key]
        public int PedidoId { get; set; }

        [Required]
        public DateTime FechaPedido { get; set; } = DateTime.Now;

        public DateTime? FechaEnvio { get; set; }

        public DateTime? FechaEntrega { get; set; }

        public DateTime? FechaCancelado { get; set; }

        [Required]
        public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

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

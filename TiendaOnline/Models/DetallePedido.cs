﻿using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class DetallePedido
    {
        [Key]
        public int DetallePedidoId { get; set; }

        public int PedidoId { get; set; }

        public int ProductoId { get; set; }
        public Producto Producto { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Precision(18, 2)]
        [Range(0, double.MaxValue)]
        public decimal PrecioUnitario { get; set; }
    }

}

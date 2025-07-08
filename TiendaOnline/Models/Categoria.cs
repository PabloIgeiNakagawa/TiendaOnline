using System.ComponentModel.DataAnnotations;

namespace TiendaOnline.Models
{
    public class Categoria
    {
        [Key]
        public int CategoriaId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        public ICollection<Producto> Productos { get; set; }
    }
}

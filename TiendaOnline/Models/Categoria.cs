using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TiendaOnline.Models
{
    public class Categoria
    {
        [Key]
        public int CategoriaId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        public int? CategoriaPadreId { get; set; }

        [ForeignKey("CategoriaPadreId")]
        public virtual Categoria? CategoriaPadre { get; set; }

        public bool Activa { get; set; } = true;

        public virtual ICollection<Categoria> Subcategorias { get; set; } = new List<Categoria>();
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}
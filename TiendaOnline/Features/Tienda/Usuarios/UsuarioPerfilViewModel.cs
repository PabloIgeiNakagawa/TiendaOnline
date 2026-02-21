namespace TiendaOnline.Features.Tienda.Usuarios
{
    public class UsuarioPerfilViewModel
    {
        public int UsuarioId { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public DateTime FechaCreacion { get; set; }
        public bool Activo { get; set; }
        public DateTime? UltimaFechaBaja { get; set; }
        public DateTime? UltimaFechaAlta { get; set; }
        public bool esPropioPerfil { get; set; } // Para saber si el usuario está viendo su propio perfil o el de otro

        // Aquí puedes agregar los datos que antes estaban hardcodeados
        public int CantidadPedidos { get; set; }
        public decimal TotalGastado { get; set; }
    }
}

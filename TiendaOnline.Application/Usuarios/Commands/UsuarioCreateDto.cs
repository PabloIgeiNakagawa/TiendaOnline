namespace TiendaOnline.Application.Usuarios.Commands
{
    public class UsuarioCreateDto
    {
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public DateTime FechaNacimiento { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Contrasena { get; set; }
        public int RolId { get; set; }
    }
}

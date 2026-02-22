namespace TiendaOnline.Features.Admin.Usuarios
{
    public class UsuarioListadoDto
    {
        public int UsuarioId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string RolNombre { get; set; } = string.Empty;
        public bool Activo { get; set; }

        // Propiedad de ayuda para el ID formateado que usás en la vista (#000001)
        public string UsuarioIdFormateado => $"#{UsuarioId.ToString("D6")}";
    }
}

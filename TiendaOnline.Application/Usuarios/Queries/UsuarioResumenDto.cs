namespace TiendaOnline.Application.Usuarios.Queries
{
    /// <summary>
    /// Los campos que la Web arma en los forms de perfil (editar usuario y
    /// cambiar contraseña): existencia + identidad. Le entra entidad del Domain
    /// al controller ni a las vistas — el query service proyecta solo esto.
    /// </summary>
    public record UsuarioResumenDto(int UsuarioId, string Nombre, string Apellido, string Telefono);
}

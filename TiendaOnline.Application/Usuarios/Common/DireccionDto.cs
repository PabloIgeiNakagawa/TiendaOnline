namespace TiendaOnline.Application.Usuarios.Common
{
    public class DireccionDto
    {
        public int DireccionId { get; set; }
        public int UsuarioId { get; set; }
        public string Etiqueta { get; set; }
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string? Piso { get; set; }
        public string? Departamento { get; set; }
        public string Localidad { get; set; }
        public string Provincia { get; set; }
        public string CodigoPostal { get; set; }
        public string? Observaciones { get; set; }
        public bool EsPrincipal { get; set; }
        public bool Activo { get; set; }
    }
}

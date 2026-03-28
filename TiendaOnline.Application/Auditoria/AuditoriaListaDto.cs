namespace TiendaOnline.Application.Auditoria
{
    public class AuditoriaListaDto
    {
        public int AuditoriaId { get; set; }
        public DateTime Fecha { get; set; }
        public string Accion { get; set; }
        public string TablaAfectada { get; set; }
        public string UsuarioNombreCompleto { get; set; }
        public string UsuarioEmail { get; set; }
    }
}

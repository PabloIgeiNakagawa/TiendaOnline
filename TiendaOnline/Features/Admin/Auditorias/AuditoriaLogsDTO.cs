namespace TiendaOnline.Features.Admin.Auditorias
{
    // Para la tabla principal
    public class AuditoriaListaDto
    {
        public int AuditoriaId { get; set; }
        public DateTime Fecha { get; set; }
        public string Accion { get; set; }
        public string TablaAfectada { get; set; }
        public string UsuarioNombreCompleto { get; set; }
        public string UsuarioEmail { get; set; }
    }

    // Para el modal de detalles
    public class AuditoriaDetalleDto
    {
        public string DatosAnteriores { get; set; }
        public string DatosNuevos { get; set; }
        public string EntidadId { get; set; }
    }
}

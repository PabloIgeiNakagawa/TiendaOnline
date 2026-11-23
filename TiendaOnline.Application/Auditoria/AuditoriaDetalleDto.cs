namespace TiendaOnline.Application.Auditoria
{
    public class AuditoriaDetalleDto
    {
        public string Accion { get; set; }
        public string TablaAfectada { get; set; }
        public string DatosAnteriores { get; set; }
        public string DatosNuevos { get; set; }
        public string EntidadId { get; set; }
    }
}

namespace TiendaOnline.Services.DTOs.Usuario
{
    public class DireccionDto
    {
        public string Etiqueta { get; set; }
        public string Calle { get; set; }
        public string Numero { get; set; }
        public string? Piso { get; set; }
        public string? Departamento { get; set; }
        public string Localidad { get; set; }
        public string Provincia { get; set; }
        public string CodigoPostal { get; set; }
    }
}

namespace TiendaOnline.Application.AppSettings
{
    /// <summary>
    /// Claves de AppSettings: contrato único para las semillas de DbInitializer y
    /// las lecturas del código. Un typo en un literal devolvería null en silencio
    /// (nombre del sitio, CBU de transferencia, email de avisos).
    /// </summary>
    public static class AppSettingsKeys
    {
        // Diseño: lo leen el panel, los emails, el PDF y la preferencia de MP.
        public const string NombreDelSitio = "Diseno:NombreDelSitio";
        public const string LogoUrl = "Diseno:LogoUrl";
        public const string FavIconUrl = "Diseno:FavIconUrl";
        public const string ColorPrimary = "Diseno:ColorPrimary";
        public const string FuenteTitulo = "Diseno:FuenteTitulo";
        public const string FuenteBody = "Diseno:FuenteBody";

        // Transferencias bancarias.
        public const string TransferenciaCbu = "Transferencia:Cbu";
        public const string TransferenciaAlias = "Transferencia:Alias";
        public const string TransferenciaTitular = "Transferencia:Titular";
        public const string PagosEmailNotificaciones = "Pagos:EmailNotificaciones";

        // Datos fiscales del comprobante interno (PDF).
        public const string EmpresaRazonSocial = "Empresa:RazonSocial";
        public const string EmpresaCuit = "Empresa:Cuit";
        public const string EmpresaCondicionIva = "Empresa:CondicionIva";
        public const string EmpresaIngresosBrutos = "Empresa:IngresosBrutos";
        public const string EmpresaInicioActividades = "Empresa:InicioActividades";

        // Contacto: botón flotante, footer, FAQ y página de contacto.
        public const string TiendaTelefono = "Tienda:Telefono";
        public const string TiendaWhatsapp = "Tienda:Whatsapp";
        public const string TiendaEmailDeContacto = "Tienda:EmailDeContacto";
        public const string TiendaHorarioDeAtencion = "Tienda:HorarioDeAtencion";

        // Ubicación física del local (retiro en sucursal y footer).
        public const string UbicacionDireccion = "Ubicacion:Direccion";
        public const string UbicacionCiudad = "Ubicacion:Ciudad";
        public const string UbicacionProvincia = "Ubicacion:Provincia";
        public const string UbicacionCodigoPostal = "Ubicacion:CodigoPostal";

        // SEO de los layouts.
        public const string SeoTitulo = "Seo:Titulo";
        public const string SeoDescripcion = "Seo:Descripcion";
        public const string SeoPalabrasClaves = "Seo:Palabras claves";
        public const string SeoAutor = "Seo:Autor";

        // Redes sociales del footer.
        public const string RedesFacebook = "RedesSociales:Facebook";
        public const string RedesInstagram = "RedesSociales:Instagram";
        public const string RedesX = "RedesSociales:X";
        public const string RedesLinkedin = "RedesSociales:Linkedin";
        public const string RedesTiktok = "RedesSociales:Tiktok";
        public const string RedesYoutube = "RedesSociales:Youtube";

        // Open Graph de los layouts internos (no sembradas en DbInitializer).
        public const string OpenGraphTitulo = "OpenGraph:Titulo";
        public const string OpenGraphDescripcion = "OpenGraph:Descripcion";
        public const string OpenGraphImagenUrl = "OpenGraph:ImagenUrl";
    }
}

using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static void SeedSettings(TiendaContext context)
        {
            // Lista de configuraciones iniciales necesarias para que la web funcione
            var defaultSettings = new List<AppSetting>
            {
                // === Pagos ===
                new AppSetting { Key = "Transferencia:Cbu", Group = "Pagos", IsSensitive = false, Description = "Número de CBU para transferencias bancarias.", Type = "text", LastModified = null },
                new AppSetting { Key = "Transferencia:Alias", Group = "Pagos", IsSensitive = false, Description = "Alias del CBU para recibir pagos.", Type = "text", LastModified = null },
                new AppSetting { Key = "Transferencia:Titular", Group = "Pagos", IsSensitive = false, Description = "Nombre del titular de la cuenta para transferencias.", Type = "text", LastModified = null },
                
                // === SEO ===
                new AppSetting { Key = "Seo:Titulo", Group = "Seo", IsSensitive = false, Description = "Título por defecto para SEO del sitio.", Type = "text", LastModified = null },
                new AppSetting { Key = "Seo:Descripcion", Group = "Seo", IsSensitive = false, Description = "Descripción por defecto para la meta descripción del sitio.", Type = "textarea", LastModified = null },
                new AppSetting { Key = "Seo:Palabras claves", Group = "Seo", IsSensitive = false, Description = "Palabras clave separadas por comas para SEO.", Type = "text", LastModified = null },
                new AppSetting { Key = "Seo:Autor", Group = "Seo", IsSensitive = false, Description = "Nombre del autor o entidad responsable del sitio.", Type = "text", LastModified = null },
            
                // === Contacto, Ubicación y Redes sociales ===
                new AppSetting { Key = "Tienda:Telefono", Group = "Contacto", IsSensitive = false, Description = "Número de teléfono de la tienda.", Type = "tel", LastModified = null },
                new AppSetting { Key = "Tienda:EmailDeContacto", Group = "Contacto", IsSensitive = false, Description = "Correo electrónico de contacto principal.", Type = "email", LastModified = null },
                new AppSetting { Key = "Tienda:Whatsapp", Group = "Contacto", IsSensitive = false, Description = "Número de WhatsApp para atención al cliente.", Type = "tel", LastModified = null },
                new AppSetting { Key = "Tienda:HorarioDeAtencion", Group = "Contacto", IsSensitive = false, Description = "Horario de atención al cliente.", Type = "text", LastModified = null },
                new AppSetting { Key = "Ubicacion:Direccion", Group = "Contacto", IsSensitive = false, Description = "Dirección física de la tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = "Ubicacion:Ciudad", Group = "Contacto", IsSensitive = false, Description = "Ciudad donde se ubica la tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = "Ubicacion:Provincia", Group = "Contacto", IsSensitive = false, Description = "Provincia o estado de la tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = "Ubicacion:CodigoPostal", Group = "Contacto", IsSensitive = false, Description = "Código postal de la ubicación de la tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = "RedesSociales:Instagram", Group = "Contacto", IsSensitive = false, Description = "URL del perfil de Instagram.", Type = "url", LastModified = null },
                new AppSetting { Key = "RedesSociales:Linkedin", Group = "Contacto", IsSensitive = false, Description = "URL del perfil de LinkedIn.", Type = "url", LastModified = null },
                new AppSetting { Key = "RedesSociales:Youtube", Group = "Contacto", IsSensitive = false, Description = "URL del canal de YouTube.",     Type = "url", LastModified = null },
                new AppSetting { Key = "RedesSociales:Tiktok", Group = "Contacto", IsSensitive = false, Description = "URL del perfil de TikTok.", Type = "url", LastModified = null },
                new AppSetting { Key = "RedesSociales:Facebook", Group = "Contacto", IsSensitive = false, Description = "URL de la página de Facebook.", Type = "url", LastModified = null },
                new AppSetting { Key = "RedesSociales:X", Group = "Contacto", IsSensitive = false, Description = "URL del perfil en X (antes Twitter).", Type = "url", LastModified = null },

                // === Branding & Estética ===
                new AppSetting { Key = "Diseno:NombreDelSitio", Group = "Estetica", IsSensitive = false, Description = "Nombre público del sitio o tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = "Diseno:LogoUrl", Group = "Estetica", IsSensitive = false, Description = "URL del logo principal del sitio.", Type = "url", LastModified = null },
                new AppSetting { Key = "Diseno:FavIconUrl", Group = "Estetica", IsSensitive = false, Description = "URL del favicon del sitio.", Type = "url", LastModified = null },
                new AppSetting { Key = "Diseno:ColorPrimary", Group = "Estetica", IsSensitive = false, Description = "Color primario para la interfaz.", Type = "color", LastModified = null },
                new AppSetting { Key = "Diseno:FuenteTitulo", Group = "Estetica", IsSensitive = false, Description = "Fuente tipográfica utilizada para títulos.", Type = "select", LastModified = null },
                new AppSetting { Key = "Diseno:FuenteBody", Group = "Estetica", IsSensitive = false, Description = "Fuente tipográfica utilizada para el cuerpo del contenido.", Type = "select", LastModified = null }
            };

            foreach (var setting in defaultSettings)
            {
                // Solo la agregamos si no existe ya en la DB
                if (!context.AppSettings.Any(s => s.Key == setting.Key))
                {
                    context.AppSettings.Add(setting);
                }
            }
            context.SaveChanges();
        }
    }
}

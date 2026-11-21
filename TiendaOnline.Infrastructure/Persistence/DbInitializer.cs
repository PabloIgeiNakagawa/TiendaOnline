using TiendaOnline.Application.AppSettings;
using TiendaOnline.Domain.Entities;

namespace TiendaOnline.Infrastructure.Persistence
{
    public static class DbInitializer
    {
        public static void SeedSettings(TiendaContext context)
        {
            var defaultSettings = new List<AppSetting>
            {
                new AppSetting { Key = AppSettingsKeys.TransferenciaCbu, Group = "Pagos", IsSensitive = false, Description = "Número de CBU para transferencias bancarias.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.TransferenciaAlias, Group = "Pagos", IsSensitive = false, Description = "Alias del CBU para recibir pagos.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.TransferenciaTitular, Group = "Pagos", IsSensitive = false, Description = "Nombre del titular de la cuenta para transferencias.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.PagosEmailNotificaciones, Group = "Pagos", IsSensitive = true, Description = "Email interno que recibe el aviso cuando un cliente sube el comprobante de transferencia.", Type = "text", LastModified = null },

                new AppSetting { Key = AppSettingsKeys.SeoTitulo, Group = "Seo", IsSensitive = false, Description = "Título por defecto para SEO del sitio.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.SeoDescripcion, Group = "Seo", IsSensitive = false, Description = "Descripción por defecto para la meta descripción del sitio.", Type = "textarea", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.SeoPalabrasClaves, Group = "Seo", IsSensitive = false, Description = "Palabras clave separadas por comas para SEO.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.SeoAutor, Group = "Seo", IsSensitive = false, Description = "Nombre del autor o entidad responsable del sitio.", Type = "text", LastModified = null },

                new AppSetting { Key = AppSettingsKeys.TiendaTelefono, Group = "Contacto", IsSensitive = false, Description = "Número de teléfono de la tienda.", Type = "tel", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.TiendaEmailDeContacto, Group = "Contacto", IsSensitive = false, Description = "Correo electrónico de contacto principal.", Type = "email", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.TiendaWhatsapp, Group = "Contacto", IsSensitive = false, Description = "Número de WhatsApp para atención al cliente.", Type = "tel", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.TiendaHorarioDeAtencion, Group = "Contacto", IsSensitive = false, Description = "Horario de atención al cliente.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.UbicacionDireccion, Group = "Contacto", IsSensitive = false, Description = "Dirección física de la tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.UbicacionCiudad, Group = "Contacto", IsSensitive = false, Description = "Ciudad donde se ubica la tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.UbicacionProvincia, Group = "Contacto", IsSensitive = false, Description = "Provincia o estado de la tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.UbicacionCodigoPostal, Group = "Contacto", IsSensitive = false, Description = "Código postal de la ubicación de la tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.RedesInstagram, Group = "Contacto", IsSensitive = false, Description = "URL del perfil de Instagram.", Type = "url", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.RedesLinkedin, Group = "Contacto", IsSensitive = false, Description = "URL del perfil de LinkedIn.", Type = "url", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.RedesYoutube, Group = "Contacto", IsSensitive = false, Description = "URL del canal de YouTube.", Type = "url", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.RedesTiktok, Group = "Contacto", IsSensitive = false, Description = "URL del perfil de TikTok.", Type = "url", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.RedesFacebook, Group = "Contacto", IsSensitive = false, Description = "URL de la página de Facebook.", Type = "url", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.RedesX, Group = "Contacto", IsSensitive = false, Description = "URL del perfil en X (antes Twitter).", Type = "url", LastModified = null },

                new AppSetting { Key = AppSettingsKeys.NombreDelSitio, Group = "Estetica", IsSensitive = false, Description = "Nombre público del sitio o tienda.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.LogoUrl, Group = "Estetica", IsSensitive = false, Description = "URL del logo principal del sitio.", Type = "url", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.FavIconUrl, Group = "Estetica", IsSensitive = false, Description = "URL del favicon del sitio.", Type = "url", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.ColorPrimary, Group = "Estetica", IsSensitive = false, Description = "Color primario para la interfaz.", Type = "color", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.FuenteTitulo, Group = "Estetica", IsSensitive = false, Description = "Fuente tipográfica utilizada para títulos.", Type = "select", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.FuenteBody, Group = "Estetica", IsSensitive = false, Description = "Fuente tipográfica utilizada para el cuerpo del contenido.", Type = "select", LastModified = null },

                new AppSetting { Key = AppSettingsKeys.EmpresaCuit, Group = "Empresa", IsSensitive = false, Description = "CUIT de la empresa para comprobantes.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.EmpresaRazonSocial, Group = "Empresa", IsSensitive = false, Description = "Razón social de la empresa.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.EmpresaCondicionIva, Group = "Empresa", IsSensitive = false, Description = "Condición frente al IVA (Responsable Inscripto, Monotributista, Exento, etc.).", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.EmpresaIngresosBrutos, Group = "Empresa", IsSensitive = false, Description = "Número de Ingresos Brutos.", Type = "text", LastModified = null },
                new AppSetting { Key = AppSettingsKeys.EmpresaInicioActividades, Group = "Empresa", IsSensitive = false, Description = "Fecha de inicio de actividades.", Type = "text", LastModified = null }
            };

            foreach (var setting in defaultSettings)
            {
                // Semilla idempotente: solo agrega la clave que todavía no existe.
                if (!context.AppSettings.Any(s => s.Key == setting.Key))
                {
                    context.AppSettings.Add(setting);
                }
            }
            context.SaveChanges();
        }

        // Las 24 provincias con el mismo nombre que devuelve la API georef (getProvincias),
        // que es la fuente del select del formulario de dirección: así la zona matchea con
        // lo que el cliente eligió sin normalizaciones raras.
        private static readonly List<string> ProvinciasArgentina = new()
        {
            "Buenos Aires",
            "Ciudad Autónoma de Buenos Aires",
            "Catamarca",
            "Chaco",
            "Chubut",
            "Córdoba",
            "Corrientes",
            "Entre Ríos",
            "Formosa",
            "Jujuy",
            "La Pampa",
            "La Rioja",
            "Mendoza",
            "Misiones",
            "Neuquén",
            "Río Negro",
            "Salta",
            "San Juan",
            "San Luis",
            "Santa Cruz",
            "Santa Fe",
            "Santiago del Estero",
            "Tierra del Fuego, Antártida e Islas del Atlántico Sur",
            "Tucumán"
        };

        // Costo con el que se siembran las 24 zonas; el admin lo edita desde /admin/envios
        // y el checkout siempre re-cotiza contra la tabla.
        public const decimal CostoEnvioInicial = 500m;

        public static void SeedZonasEnvio(TiendaContext context)
        {
            if (context.ZonasEnvio.Any())
                return;

            foreach (var provincia in ProvinciasArgentina)
            {
                context.ZonasEnvio.Add(new ZonaEnvio
                {
                    Provincia = provincia,
                    Costo = CostoEnvioInicial,
                    Activa = true
                });
            }
            context.SaveChanges();
        }
    }
}

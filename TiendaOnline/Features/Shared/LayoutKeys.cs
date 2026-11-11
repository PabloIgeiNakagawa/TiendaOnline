namespace TiendaOnline.Features.Shared
{
    /// <summary>
    /// Rutas de los tres layouts de la app. Las declara la CLASE del controller
    /// (AdminControllerBase / RepartidorControllerBase) o el action, con
    /// ViewData[ViewDataKeys.Layout]; Features/_ViewStart las consume.
    /// </summary>
    public static class LayoutKeys
    {
        /// <summary>Layout de la tienda pública (default de Features/_ViewStart).</summary>
        public const string Tienda = "~/Features/Shared/Layouts/_TiendaLayout.cshtml";

        /// <summary>Layout del panel de administración.</summary>
        public const string Admin = "~/Features/Shared/Layouts/_AdminLayout.cshtml";

        /// <summary>Layout de la cola de entregas del repartidor.</summary>
        public const string Repartidor = "~/Features/Shared/Layouts/_RepartidorLayout.cshtml";
    }
}

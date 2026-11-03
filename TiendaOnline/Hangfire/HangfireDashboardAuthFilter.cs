using Hangfire.Dashboard;

namespace TiendaOnline.Hangfire
{
    /// <summary>
    /// Solo permite abrir el dashboard /hangfire a usuarios logueados con rol Administrador.
    /// El default de Hangfire (solo requests locales/loopback) no alcanza detrás de un
    /// reverse proxy o shared hosting: la ruta puede quedar expuesta al público,
    /// mostrando emails de clientes en jobs en cola y permitiendo re-encolar operaciones.
    /// </summary>
    public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var user = context.GetHttpContext().User;
            return user?.Identity?.IsAuthenticated == true
                && user.IsInRole("Administrador");
        }
    }
}

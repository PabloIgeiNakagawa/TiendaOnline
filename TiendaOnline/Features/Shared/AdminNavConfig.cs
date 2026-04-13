using TiendaOnline.Features.Shared;

namespace TiendaOnline.Features.Shared
{
    public static class AdminNavConfig
    {
        public static List<AdminNavLink> GestionTienda => new()
        {
            new() { Controller = "AdminReportes", Action = "Dashboard", Icon = "bi-bar-chart-line", Text = "Reportes" },
            new() { Controller = "AdminProductos", Action = "Catalogo", Icon = "bi-box-seam", Text = "Productos" },
            new() { Controller = "Categorias", Action = "Listado", Icon = "bi-tags", Text = "Categorías" },
            new() { Controller = "AdminUsuarios", Action = "Listado", Icon = "bi-people", Text = "Usuarios" }
        };

        public static List<AdminNavLink> Operaciones => new()
        {
            new() { Controller = "AdminPedidos", Action = "Listado", Icon = "bi-cart-check", Text = "Pedidos" },
            new() { Controller = "AdminAuditorias", Action = "Logs", Icon = "bi-shield-check", Text = "Auditoría" },
            new() { Controller = "MovimientosStock", Action = "Movimientos", Icon = "bi-arrow-left-right", Text = "Stock" }
        };

        public static List<AdminNavLink> Configuracion => new()
        {
            new() { Controller = "AppSettings", Action = "Estetica", Icon = "bi-gear", Text = "Apariencia" },
            new() { Controller = "AppSettings", Action = "Index", Icon = "bi-chat-dots", Text = "Contacto", GroupName = "Contacto" },
            new() { Controller = "AppSettings", Action = "Index", Icon = "bi-search", Text = "SEO", GroupName = "SEO" },
            new() { Controller = "AppSettings", Action = "Index", Icon = "bi-credit-card", Text = "Pagos", GroupName = "Pagos" }
        };
    }
}

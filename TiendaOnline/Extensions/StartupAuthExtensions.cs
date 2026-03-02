namespace TiendaOnline.Extensions
{
    public static class StartupAuthExtensions
    {
        public static IServiceCollection AddCustomSecurity(this IServiceCollection services)
        {
            // Sesión
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Cookies
            services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", config => {
                    config.LoginPath = "/Account/Login";
                    config.LogoutPath = "/Account/Logout";
                    config.Cookie.HttpOnly = true;
                    #if DEBUG
                        config.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    #else
                        config.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
                    #endif
                    config.Cookie.SameSite = SameSiteMode.Strict;
                });

            return services;
        }
    }
}

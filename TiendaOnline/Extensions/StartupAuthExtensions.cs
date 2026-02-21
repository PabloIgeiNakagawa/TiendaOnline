namespace TiendaOnline.Extensions
{
    public static class StartupAuthExtensions
    {
        public static IServiceCollection AddCustomSecurity(this IServiceCollection services)
        {
            // Sesión
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Cookies
            services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", config => {
                    config.LoginPath = "/Usuario/Login";
                    config.LogoutPath = "/Usuario/Logout";
                    config.Cookie.HttpOnly = true;
                    #if DEBUG
                        config.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    #else
                        /* En producción, hay que usar Always para asegurar que las cookies solo se envíen a través de HTTPS. 
                            Pero por el somee lo dejamos en SameAsRequest para evitar problemas. */ 
                        config.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; 
                    #endif
                    config.Cookie.SameSite = SameSiteMode.Strict;
                });

            return services;
        }
    }
}

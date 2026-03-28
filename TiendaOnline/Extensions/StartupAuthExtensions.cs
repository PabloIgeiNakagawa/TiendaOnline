using Microsoft.AspNetCore.Authorization;
using TiendaOnline.Authorization;

namespace TiendaOnline.Extensions
{
    public static class StartupAuthExtensions
    {
        public static IServiceCollection AddCustomSecurity(this IServiceCollection services, IConfiguration config)
        {
            // Authorization
            services.AddAuthorization(options =>
            {
                // Para el perfil del usuario (ID Usuario == ID URL)
                options.AddPolicy("EsDuenioDelPerfil", policy =>
                    policy.Requirements.Add(new IsOwnerRequirement()));

                // Para los pedidos (ID Usuario == Pedido.UsuarioId en DB)
                options.AddPolicy("EsDuenioDelPedido", policy =>
                    policy.Requirements.Add(new IsOrderOwnerRequirement()));
            });

            services.AddScoped<IAuthorizationHandler, IsOwnerHandler>();
            services.AddScoped<IAuthorizationHandler, OrderOwnerHandler>();

            // Sesión
            services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(120);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Cookies
            services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", config => {
                    config.LoginPath = "/Accounts/Login";
                    config.LogoutPath = "/Accounts/Logout";
                    config.AccessDeniedPath = "/Home/AccesoDenegado";
                    config.Cookie.HttpOnly = true;
                    #if DEBUG
                        config.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                    #else
                        config.Cookie.SecurePolicy = CookieSecurePolicy.Always; 
                    #endif
                    config.Cookie.SameSite = SameSiteMode.Lax;
                })
                .AddCookie("ExternalCookie", options =>  // <-- Esquema temporal para Google
                {
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SameSite = SameSiteMode.Lax; // Crucial para que Google pueda escribirla
                })
                .AddGoogle(options =>
                {
                    options.SignInScheme = "ExternalCookie"; // <-- Le decimos a Google que use el temporal
                    options.ClientId = config["GoogleKeys:ClientId"] ?? string.Empty;
                    options.ClientSecret = config["GoogleKeys:ClientSecret"] ?? string.Empty;
                });

            return services;
        }
    }
}

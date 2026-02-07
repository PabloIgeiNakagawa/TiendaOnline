using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using TiendaOnline.Data;
using TiendaOnline.Services.DTOs.Admin.Home;
using TiendaOnline.Services.IServices.Admin;

namespace TiendaOnline.Services.Services.Admin
{
    public class HomeService : IHomeService
    {
        private readonly TiendaContext _context;
        private readonly IConfiguration _config;

        public HomeService(TiendaContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<IndexDTO> ObtenerResumenHomeAsync()
        {
            var dto = new IndexDTO
            {
                DbOnline = await VerificarEstadoBaseDatosAsync(),
                AppVersion = ObtenerVersionApp(),
                Environment = _config["Environment"] ?? "Production"
            };

            // Timeline de Auditoría (Últimos 5 cambios de productos o precios)
            dto.UltimosCambios = await _context.Auditorias
                .OrderByDescending(a => a.Fecha)
                .Take(5)
                .Select(a => new AuditoriaEntryDTO
                {
                    UsuarioNombre = $"{a.Usuario.Nombre} {a.Usuario.Apellido}",
                    Accion = a.Accion,
                    Fecha = a.Fecha
                }).ToListAsync();

            // Pedidos Estancados (Estado 0: Pendiente y más de 48hs)
            var limiteFecha = DateTime.Now.AddHours(-48);
            dto.PedidosEstancados = await _context.Pedidos
                .Where(p => p.Estado == 0 && p.FechaPedido <= limiteFecha)
                .Select(p => new PedidoEstancadoDTO
                {
                    PedidoId = p.PedidoId,
                    ClienteNombre = $"{p.Usuario.Nombre} {p.Usuario.Apellido}",
                    Fecha = p.FechaPedido,
                    HorasTranscurridas = (DateTime.Now - p.FechaPedido).TotalHours
                }).ToListAsync();

            return dto;
        }

        public async Task<bool> VerificarEstadoBaseDatosAsync()
        {
            try
            {
                return await _context.Database.CanConnectAsync();
            }
            catch
            {
                return false;
            }
        }

        public string ObtenerVersionApp()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            return version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "1.0.0";
        }
    }
}

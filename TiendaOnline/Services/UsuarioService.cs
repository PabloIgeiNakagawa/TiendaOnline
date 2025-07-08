using TiendaOnline.Data;
using TiendaOnline.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TiendaOnline.Exceptions;
using System.Diagnostics;
using TiendaOnline.IServices;
using Newtonsoft.Json;

namespace TiendaOnline.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly TiendaContext _context;
        private readonly PasswordHasher<Usuario> _passwordHasher;
        private readonly IAuditoriaService _auditoriaService;
        private readonly int usuarioActivoId;

        public UsuarioService(TiendaContext context, IAuditoriaService auditoriaService)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<Usuario>();
            _auditoriaService = auditoriaService;
            usuarioActivoId = _auditoriaService.ObtenerUsuarioId();
        }

        public async Task<Usuario?> ObtenerPorEmailAsync(string email)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<Usuario>> ObtenerUsuariosAsync()
        {
            return await _context.Usuarios.ToListAsync();
        }

        public async Task<Usuario?> ObtenerUsuarioAsync(int usuarioId)
        {
            return await _context.Usuarios.FirstOrDefaultAsync(u => u.UsuarioId == usuarioId);
        }

        public async Task CrearUsuarioAsync(Usuario usuario)
        {
            var emailExiste = await _context.Usuarios.AnyAsync(u => u.Email == usuario.Email);
            if (emailExiste)
            {
                throw new EmailDuplicadoException(usuario.Email);
            }

            usuario.Contrasena = _passwordHasher.HashPassword(usuario, usuario.Contrasena);
            usuario.Email.Trim().ToLower();

            _context.Usuarios.Add(usuario);
            int cambios = await _context.SaveChangesAsync();
            if (cambios > 0)
            {
                var auditoria = new Auditoria
                {
                    UsuarioId = usuarioActivoId,
                    Accion = "Crear Usuario",
                    DatosAnteriores = "{}",
                    DatosNuevos = JsonConvert.SerializeObject(usuario),
                    Fecha = DateTime.Now
                };
                await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
            }
            else
            {
                throw new Exception("No se pudo crear el usuario.");
            }
        }

        public async Task DarBajaUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                throw new Exception("Usuario no encontrado.");
            DateTime? fechaAnterior = usuario.UltimaFechaBaja;

            usuario.Activo = false;
            usuario.UltimaFechaBaja = DateTime.Now;

            int cambios = await _context.SaveChangesAsync();
            if (cambios > 0)
            {
                var auditoria = new Auditoria
                {
                    UsuarioId = usuarioActivoId,
                    Accion = "Dar baja usuario",
                    DatosAnteriores = JsonConvert.SerializeObject(new { Activo = true, UltimaFechaBaja = fechaAnterior }),
                    DatosNuevos = JsonConvert.SerializeObject(new { Activo = false, UltimaFechaBaja = usuario.UltimaFechaBaja }),
                    Fecha = DateTime.Now
                };
                await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
            }
            else
            {
                throw new Exception("No se pudo dar de baja el usuario.");
            }
        }

        public async Task DarAltaUsuarioAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                throw new Exception("Usuario no encontrado.");

            DateTime? fechaAnterior = usuario.UltimaFechaAlta;

            usuario.Activo = true;
            usuario.UltimaFechaAlta = DateTime.Now;

            int cambios = await _context.SaveChangesAsync();
            if (cambios > 0)
            {
                var auditoria = new Auditoria
                {
                    UsuarioId = usuarioActivoId,
                    Accion = "Dar alta usuario",
                    DatosAnteriores = JsonConvert.SerializeObject(new { Activo = false, UltimaFechaAlta = fechaAnterior }),
                    DatosNuevos = JsonConvert.SerializeObject(new { Activo = true, ultimaFechaAlta = usuario.UltimaFechaAlta }),
                    Fecha = DateTime.Now
                };
                await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
            }
            else
            {
                throw new Exception("No se pudo dar de alta el usuario.");
            }
        }

        public async Task EditarUsuarioAsync(int usuarioId, Usuario usuarioEditado)
        {
            var usuario = await _context.Usuarios.FindAsync(usuarioId);
            if (usuario == null)
                throw new Exception("Usuario no encontrado.");

            var datosAnteriores = new Usuario
            {
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Telefono = usuario.Telefono,
                Direccion = usuario.Direccion,
                FechaNacimiento = usuario.FechaNacimiento,
                Email = usuario.Email,
                Contrasena = usuario.Contrasena,
                ConfirmarContrasena = usuario.ConfirmarContrasena
            };

            usuario.Nombre = usuarioEditado.Nombre;
            usuario.Apellido = usuarioEditado.Apellido;
            usuario.Telefono = usuarioEditado.Telefono;
            usuario.Direccion = usuarioEditado.Direccion;

            int cambios = await _context.SaveChangesAsync();
            if (cambios > 0)
            {
                var auditoria = new Auditoria
                {
                    UsuarioId = usuarioActivoId,
                    Accion = "Editar usuario",
                    DatosAnteriores = JsonConvert.SerializeObject(datosAnteriores),
                    DatosNuevos = JsonConvert.SerializeObject(usuario),
                    Fecha = DateTime.Now
                };
                await _auditoriaService.RegistrarAuditoriaAsync(auditoria);
            }
            else
            {
                throw new Exception("No se pudo editar el usuario.");
            }
        }
    }
}

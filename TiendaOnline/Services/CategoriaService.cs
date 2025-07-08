using Microsoft.EntityFrameworkCore;
using TiendaOnline.Data;
using TiendaOnline.IServices;
using TiendaOnline.Models;

namespace TiendaOnline.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly TiendaContext _context;

        public CategoriaService(TiendaContext context)
        {
            _context = context;
        }

        public async Task<Categoria?> ObtenerCategoriaAsync(int id)
        {
            return await _context.Categorias.FindAsync(id);
        }

        public async Task<List<Categoria>> ObtenerCategoriasAsync()
        {
            return await _context.Categorias.ToListAsync();
        }
    }
}

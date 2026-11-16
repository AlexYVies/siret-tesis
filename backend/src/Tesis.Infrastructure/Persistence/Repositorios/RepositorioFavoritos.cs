using Microsoft.EntityFrameworkCore;
using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;

namespace Tesis.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioFavoritos : IFavoritoRepository
{
    private readonly TesisDbContext _contexto;

    public RepositorioFavoritos(TesisDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<bool> ExisteAsync(int idUsuario, Guid folio, CancellationToken ct = default) =>
        _contexto.Favoritos.AnyAsync(f => f.IdUsuario == idUsuario && f.Folio == folio, ct);

    public async Task<List<Favorito>> ObtenerDeUsuarioAsync(int idUsuario, CancellationToken ct = default) =>
        await _contexto.Favoritos
            .Include(f => f.Tesis!)
            .Include(f => f.Tesis!.Alumno)
            .Include(f => f.Tesis!.Asesor)
            .Include(f => f.Tesis!.Dictamenes)
            .Where(f => f.IdUsuario == idUsuario)
            .OrderByDescending(f => f.FechaReg)
            .ToListAsync(ct);

    public void Agregar(Favorito favorito) =>
        _contexto.Favoritos.Add(favorito);

    public async Task<bool> EliminarAsync(int idUsuario, Guid folio, CancellationToken ct = default)
    {
        var favorito = await _contexto.Favoritos
            .FirstOrDefaultAsync(f => f.IdUsuario == idUsuario && f.Folio == folio, ct);

        if (favorito is null)
        {
            return false;
        }

        _contexto.Favoritos.Remove(favorito);
        return true;
    }
}
using Microsoft.EntityFrameworkCore;
using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;
using Tesis.Infrastructure.Persistence;

namespace Tesis.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioPosgrados : IPosgradoRepository
{
    private readonly TesisDbContext _contexto;

    public RepositorioPosgrados(TesisDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<List<Posgrado>> ObtenerTodosAsync(CancellationToken ct = default) =>
        await _contexto.Posgrados
            .OrderBy(p => p.Nivel)
            .ThenBy(p => p.Nombre)
            .ToListAsync(ct);

    public async Task<List<Posgrado>> ObtenerActivosAsync(CancellationToken ct = default) =>
        await _contexto.Posgrados
            .Where(p => p.Activo)
            .OrderBy(p => p.Nivel)
            .ThenBy(p => p.Nombre)
            .ToListAsync(ct);

    public async Task<Posgrado?> ObtenerPorIdAsync(int idPosgrado, CancellationToken ct = default) =>
        await _contexto.Posgrados
            .FirstOrDefaultAsync(p => p.IdPosgrado == idPosgrado, ct);

    public async Task<Posgrado?> ObtenerPorNombreYNivelAsync(string nombre, NivelPosgrado nivel, CancellationToken ct = default) =>
        await _contexto.Posgrados
            .FirstOrDefaultAsync(p => p.Nivel == nivel && p.Nombre.ToLower() == nombre.Trim().ToLower(), ct);

    public async Task AgregarAsync(Posgrado posgrado, CancellationToken ct = default)
    {
        await _contexto.Posgrados.AddAsync(posgrado, ct);
    }

    public Task EliminarAsync(Posgrado posgrado, CancellationToken ct = default)
    {
        _contexto.Posgrados.Remove(posgrado);
        return Task.CompletedTask;
    }
}

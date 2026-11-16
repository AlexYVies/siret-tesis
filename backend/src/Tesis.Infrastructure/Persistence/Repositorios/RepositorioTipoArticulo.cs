using Microsoft.EntityFrameworkCore;
using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;
using Tesis.Infrastructure.Persistence;

namespace Tesis.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioTipoArticulo : ITipoArticuloRepository
{
    private readonly TesisDbContext _contexto;

    public RepositorioTipoArticulo(TesisDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<List<TipoArticulo>> ObtenerActivosAsync(CancellationToken ct = default) =>
        await _contexto.TiposArticulo
            .Where(t => t.Activo)
            .OrderBy(t => t.Nombre)
            .ToListAsync(ct);

    public Task<TipoArticulo?> ObtenerPorNombreAsync(string nombre, CancellationToken ct = default) =>
        _contexto.TiposArticulo
            .FirstOrDefaultAsync(t => t.Nombre.ToLower() == nombre.ToLower().Trim(), ct);

    public async Task AgregarAsync(TipoArticulo tipoArticulo, CancellationToken ct = default)
    {
        await _contexto.TiposArticulo.AddAsync(tipoArticulo, ct);
    }
}
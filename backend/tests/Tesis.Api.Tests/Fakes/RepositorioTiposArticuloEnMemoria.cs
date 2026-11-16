using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;

namespace Tesis.Application.Tests.Fakes;

public sealed class RepositorioTiposArticuloEnMemoria : ITipoArticuloRepository
{
    private readonly List<TipoArticulo> _tipos = new();
    private int _siguienteId = 1;

    public Task<List<TipoArticulo>> ObtenerActivosAsync(CancellationToken ct = default) =>
        Task.FromResult(_tipos.Where(t => t.Activo).OrderBy(t => t.Nombre).ToList());

    public Task<TipoArticulo?> ObtenerPorNombreAsync(string nombre, CancellationToken ct = default) =>
        Task.FromResult(_tipos.FirstOrDefault(t =>
            string.Equals(t.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task AgregarAsync(TipoArticulo tipoArticulo, CancellationToken ct = default)
    {
        if (tipoArticulo.IdTipoArticulo == 0)
        {
            tipoArticulo.IdTipoArticulo = _siguienteId++;
        }

        _tipos.Add(tipoArticulo);
        return Task.CompletedTask;
    }
}
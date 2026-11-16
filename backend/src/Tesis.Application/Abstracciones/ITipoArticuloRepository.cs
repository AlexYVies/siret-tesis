using Tesis.Domain.Entities;

namespace Tesis.Application.Abstracciones;

public interface ITipoArticuloRepository
{
    Task<List<TipoArticulo>> ObtenerActivosAsync(CancellationToken ct = default);
    Task<TipoArticulo?> ObtenerPorNombreAsync(string nombre, CancellationToken ct = default);
    Task AgregarAsync(TipoArticulo tipoArticulo, CancellationToken ct = default);
}
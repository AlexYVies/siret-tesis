using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

namespace Tesis.Application.Abstracciones;

public interface IPosgradoRepository
{
    Task<List<Posgrado>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<List<Posgrado>> ObtenerActivosAsync(CancellationToken ct = default);
    Task<Posgrado?> ObtenerPorIdAsync(int idPosgrado, CancellationToken ct = default);
    Task<Posgrado?> ObtenerPorNombreYNivelAsync(string nombre, NivelPosgrado nivel, CancellationToken ct = default);
    Task AgregarAsync(Posgrado posgrado, CancellationToken ct = default);
    Task EliminarAsync(Posgrado posgrado, CancellationToken ct = default);
}

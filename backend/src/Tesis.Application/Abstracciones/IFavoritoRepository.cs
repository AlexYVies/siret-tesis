using Tesis.Domain.Entities;

namespace Tesis.Application.Abstracciones;

public interface IFavoritoRepository
{
    Task<bool> ExisteAsync(int idUsuario, Guid folio, CancellationToken ct = default);
    Task<List<Favorito>> ObtenerDeUsuarioAsync(int idUsuario, CancellationToken ct = default);
    void Agregar(Favorito favorito);
    Task<bool> EliminarAsync(int idUsuario, Guid folio, CancellationToken ct = default);
}
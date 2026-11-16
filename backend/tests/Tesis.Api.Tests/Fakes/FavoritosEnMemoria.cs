using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;

namespace Tesis.Application.Tests.Fakes;

public sealed class FavoritosEnMemoria : IFavoritoRepository
{
    private readonly ITesisRepository _tesisRepo;
    private readonly List<Favorito> _favoritos = new();
    private int _siguienteId = 1;

    public FavoritosEnMemoria(ITesisRepository tesisRepo)
    {
        _tesisRepo = tesisRepo;
    }

    public Task<bool> ExisteAsync(int idUsuario, Guid folio, CancellationToken ct = default) =>
        Task.FromResult(_favoritos.Any(f => f.IdUsuario == idUsuario && f.Folio == folio));

    public async Task<List<Favorito>> ObtenerDeUsuarioAsync(int idUsuario, CancellationToken ct = default)
    {
        var resultado = new List<Favorito>();
        foreach (var fav in _favoritos.Where(f => f.IdUsuario == idUsuario))
        {
            var tesis = await _tesisRepo.ObtenerPorFolioConNavegacionesAsync(fav.Folio, ct);
            if (tesis is not null)
            {
                resultado.Add(new Favorito
                {
                    IdFavorito = fav.IdFavorito,
                    IdUsuario = fav.IdUsuario,
                    Folio = fav.Folio,
                    Tesis = tesis,
                });
            }
        }

        return resultado;
    }

    public void Agregar(Favorito favorito)
    {
        favorito.IdFavorito = _siguienteId++;
        _favoritos.Add(favorito);
    }

    public Task<bool> EliminarAsync(int idUsuario, Guid folio, CancellationToken ct = default)
    {
        var favorito = _favoritos.FirstOrDefault(f => f.IdUsuario == idUsuario && f.Folio == folio);
        if (favorito is null)
        {
            return Task.FromResult(false);
        }

        _favoritos.Remove(favorito);
        return Task.FromResult(true);
    }
}
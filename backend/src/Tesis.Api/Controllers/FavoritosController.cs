using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tesis.Api.Modelos.Consultas;
using Tesis.Application.Abstracciones;
using Tesis.Application.Excepciones;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/repositorio")]
[Route("api/usuarios")]
[Authorize]
public class FavoritosController : ControllerBase
{
    private readonly IFavoritoRepository _favoritos;
    private readonly ITesisRepository _tesis;
    private readonly IUnitOfWork _unitOfWork;

    public FavoritosController(
        IFavoritoRepository favoritos,
        ITesisRepository tesis,
        IUnitOfWork unitOfWork)
    {
        _favoritos = favoritos;
        _tesis = tesis;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("{folio:guid}/favorito")]
    public async Task<IActionResult> Agregar(Guid folio, CancellationToken ct)
    {
        VerificarSesionRegistrada();
        var idUsuario = IdUsuarioActual();

        var tesis = await _tesis.ObtenerPorFolioConNavegacionesAsync(folio, ct)
            ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

        if (tesis.Estado != TesisEstado.Aceptada)
        {
            throw new OperacionNoPermitidaException("Solo las tesis aceptadas pueden marcarse como favoritas.");
        }

        if (await _favoritos.ExisteAsync(idUsuario, folio, ct))
        {
            return Ok(new { ok = true, folio, favorito = true });
        }

        _favoritos.Agregar(new Favorito { IdUsuario = idUsuario, Folio = folio });
        await _unitOfWork.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, new { ok = true, folio, favorito = true });
    }

    [HttpDelete("{folio:guid}/favorito")]
    public async Task<IActionResult> Quitar(Guid folio, CancellationToken ct)
    {
        VerificarSesionRegistrada();
        await _favoritos.EliminarAsync(IdUsuarioActual(), folio, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return Ok(new { ok = true, folio, favorito = false });
    }

    [HttpGet("{idUsuario:int}/favoritos")]
    public async Task<IActionResult> Listar(int idUsuario, CancellationToken ct)
    {
        VerificarSesionRegistrada();
        if (idUsuario != IdUsuarioActual())
        {
            throw new OperacionNoPermitidaException("Solo puede consultar sus propios favoritos.");
        }

        var favoritos = await _favoritos.ObtenerDeUsuarioAsync(idUsuario, ct);
        return Ok(favoritos
            .Where(f => f.Tesis is not null)
            .Select(f => MapearConsulta.Repositorio(f.Tesis!))
            .ToList());
    }

    private void VerificarSesionRegistrada()
    {
        var correo = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue("email");
        if (string.Equals(correo, "invitado@upt.edu.mx", StringComparison.OrdinalIgnoreCase))
        {
            throw new OperacionNoPermitidaException("Si entras sin iniciar sesión no puedes guardar favoritos. Inicia sesión o crea tu cuenta de invitado.");
        }
    }

    private int IdUsuarioActual()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(valor, out var idUsuario))
        {
            throw new UnauthorizedAccessException();
        }

        return idUsuario;
    }
}
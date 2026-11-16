using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tesis.Api.Core;
using Tesis.Api.Modelos.Consultas;
using Tesis.Application.Abstracciones;
using Tesis.Application.Excepciones;
using Tesis.Domain.Entities;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/tipos-articulo")]
public class TiposArticuloController : ControllerBase
{
    private readonly ITipoArticuloRepository _tiposArticulo;
    private readonly IUnitOfWork _unitOfWork;

    public TiposArticuloController(ITipoArticuloRepository tiposArticulo, IUnitOfWork unitOfWork)
    {
        _tiposArticulo = tiposArticulo;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var lista = await _tiposArticulo.ObtenerActivosAsync(ct);
        var dto = lista.Select(t => new TipoArticuloDto(t.IdTipoArticulo, t.Nombre, t.Descripcion));
        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = ConstantesRol.Administrador)]
    public async Task<IActionResult> Registrar(SolicitudTipoArticuloRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new SolicitudInvalidaException("El nombre del tipo de artículo es obligatorio.");
        }

        var nombre = request.Nombre.Trim();
        if (await _tiposArticulo.ObtenerPorNombreAsync(nombre, ct) is not null)
        {
            throw new ConflictoDeNegocioException($"Ya existe el tipo de artículo '{nombre}'.");
        }

        var tipoArticulo = new TipoArticulo
        {
            Nombre = nombre,
            Descripcion = string.IsNullOrWhiteSpace(request.Descripcion) ? null : request.Descripcion.Trim(),
            Activo = true,
        };

        await _tiposArticulo.AgregarAsync(tipoArticulo, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return StatusCode(
            StatusCodes.Status201Created,
            new TipoArticuloDto(tipoArticulo.IdTipoArticulo, tipoArticulo.Nombre, tipoArticulo.Descripcion));
    }
}
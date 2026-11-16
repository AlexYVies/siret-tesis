using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tesis.Api.Core;
using Tesis.Api.Modelos.Consultas;
using Tesis.Application.Abstracciones;
using Tesis.Application.Excepciones;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/alumnos")]
public class AlumnosController : ControllerBase
{
    private readonly IAlumnoRepository _alumnos;
    private readonly IUnitOfWork _unitOfWork;

    public AlumnosController(IAlumnoRepository alumnos, IUnitOfWork unitOfWork)
    {
        _alumnos = alumnos;
        _unitOfWork = unitOfWork;
    }

    [HttpGet("yo")]
    [Authorize(Roles = ConstantesRol.Alumno)]
    public async Task<IActionResult> ObtenerMiPerfil(CancellationToken ct)
    {
        var alumno = await _alumnos.ObtenerPorIdUsuarioAsync(IdUsuarioActual(), ct);
        if (alumno is null)
        {
            return NotFound(new { code = "PERFIL_NO_ENCONTRADO", message = "El usuario no tiene perfil de alumno.", details = (string[]?)null });
        }

        return Ok(MapearConsulta.Alumno(alumno));
    }

    [HttpPost]
    [Authorize(Roles = ConstantesRol.Alumno)]
    public async Task<IActionResult> CrearPerfil(SolicitudAlumnoRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new SolicitudInvalidaException("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(request.NumeroControl))
        {
            throw new SolicitudInvalidaException("El número de control es obligatorio.");
        }

        var idUsuario = IdUsuarioActual();
        if (await _alumnos.ObtenerPorIdUsuarioAsync(idUsuario, ct) is not null)
        {
            throw new ConflictoDeNegocioException("El usuario ya tiene perfil de alumno.");
        }

        var correo = (request.Correo ?? User.FindFirstValue(ClaimTypes.Name))?.Trim();
        var alumno = new Tesis.Domain.Entities.Alumno
        {
            IdUsuario = idUsuario,
            Nombre = request.Nombre.Trim(),
            NumeroControl = request.NumeroControl.Trim(),
            Correo = correo ?? string.Empty,
        };

        await _alumnos.AgregarAsync(alumno, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, MapearConsulta.Alumno(alumno));
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
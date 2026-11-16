using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tesis.Api.Core;
using Tesis.Api.Modelos.Consultas;
using Tesis.Application.Abstracciones;
using Tesis.Application.Excepciones;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/posgrados")]
public class PosgradosController : ControllerBase
{
    private readonly IPosgradoRepository _posgrados;
    private readonly IProfesorRepository _profesores;
    private readonly ITesisRepository _tesis;
    private readonly IUnitOfWork _unitOfWork;

    public PosgradosController(
        IPosgradoRepository posgrados,
        IProfesorRepository profesores,
        ITesisRepository tesis,
        IUnitOfWork unitOfWork)
    {
        _posgrados = posgrados;
        _profesores = profesores;
        _tesis = tesis;
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] bool soloActivos = false, CancellationToken ct = default)
    {
        var esAdmin = User.IsInRole(ConstantesRol.Administrador) || User.IsInRole(ConstantesRol.AdministradorDeTi);
        var filtrarSoloActivos = soloActivos || !esAdmin;

        var lista = filtrarSoloActivos
            ? await _posgrados.ObtenerActivosAsync(ct)
            : await _posgrados.ObtenerTodosAsync(ct);

        return Ok(lista.Select(MapearConsulta.Posgrado));
    }

    [HttpPost]
    [Authorize(Roles = $"{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> Registrar(SolicitudPosgradoRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new SolicitudInvalidaException("El nombre del programa de posgrado es obligatorio.");
        }

        var nombre = request.Nombre.Trim();
        var existente = await _posgrados.ObtenerPorNombreYNivelAsync(nombre, request.Nivel, ct);
        if (existente is not null)
        {
            throw new ConflictoDeNegocioException($"Ya existe el posgrado '{nombre}' con nivel {request.Nivel}.");
        }

        var posgrado = new Posgrado
        {
            Nivel = request.Nivel,
            Nombre = nombre,
            Activo = true,
        };

        await _posgrados.AgregarAsync(posgrado, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return StatusCode(StatusCodes.Status201Created, MapearConsulta.Posgrado(posgrado));
    }

    [HttpPut("{idPosgrado:int}")]
    [Authorize(Roles = $"{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> Modificar(int idPosgrado, ActualizarPosgradoRequest request, CancellationToken ct)
    {
        var posgrado = await _posgrados.ObtenerPorIdAsync(idPosgrado, ct)
            ?? throw new EntidadNoEncontradaException($"No existe el posgrado con ID {idPosgrado}.");

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new SolicitudInvalidaException("El nombre del programa de posgrado es obligatorio.");
        }

        var nuevoNombre = request.Nombre.Trim();
        var nombreCambio = !string.Equals(posgrado.Nombre, nuevoNombre, StringComparison.OrdinalIgnoreCase);
        var nivelCambio = posgrado.Nivel != request.Nivel;

        if (nombreCambio || nivelCambio)
        {
            var existente = await _posgrados.ObtenerPorNombreYNivelAsync(nuevoNombre, request.Nivel, ct);
            if (existente is not null && existente.IdPosgrado != idPosgrado)
            {
                throw new ConflictoDeNegocioException($"Ya existe otro posgrado registrado con el nombre '{nuevoNombre}' y nivel {request.Nivel}.");
            }
        }

        // Si se solicita dar de baja (o inactivar)
        var solicitarBaja = request.Activo.HasValue && !request.Activo.Value && posgrado.Activo;
        if (solicitarBaja)
        {
            var tieneEnCurso = await _tesis.TieneTesisEnCursoAsync(posgrado.Nivel, posgrado.Nombre, ct);
            if (tieneEnCurso)
            {
                throw new ConflictoDeNegocioException("No se puede dar de baja a un posgrado que tiene tesis en curso.");
            }

            // Quitar este posgrado de todos los profesores que lo imparten
            await _profesores.RemoverPosgradoDeProfesoresAsync(posgrado.Nivel, posgrado.Nombre, ct);
            posgrado.Activo = false;
        }
        else if (request.Activo.HasValue && request.Activo.Value && !posgrado.Activo)
        {
            posgrado.Activo = true;
        }

        // Si cambiaron nombre o nivel, sincronizar referencias en profesores y tesis
        if (nombreCambio || nivelCambio)
        {
            await _profesores.ActualizarProgramaEnProfesoresAsync(
                posgrado.Nivel,
                posgrado.Nombre,
                request.Nivel,
                nuevoNombre,
                ct);

            await _tesis.ActualizarProgramaEnTesisAsync(
                posgrado.Nivel,
                posgrado.Nombre,
                request.Nivel,
                nuevoNombre,
                ct);

            posgrado.Nombre = nuevoNombre;
            posgrado.Nivel = request.Nivel;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(MapearConsulta.Posgrado(posgrado));
    }

    [HttpDelete("{idPosgrado:int}")]
    [Authorize(Roles = $"{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> EliminarODarDeBaja(int idPosgrado, CancellationToken ct)
    {
        var posgrado = await _posgrados.ObtenerPorIdAsync(idPosgrado, ct)
            ?? throw new EntidadNoEncontradaException($"No existe el posgrado con ID {idPosgrado}.");

        // 1. Validar que no tenga tesis en curso
        var tieneEnCurso = await _tesis.TieneTesisEnCursoAsync(posgrado.Nivel, posgrado.Nombre, ct);
        if (tieneEnCurso)
        {
            throw new ConflictoDeNegocioException("No se puede dar de baja a un posgrado que tiene tesis en curso.");
        }

        // 2. Comprobar si tiene tesis (p. ej. ya publicadas o concluidas)
        var tieneTesis = await _tesis.TieneTesisAsync(posgrado.Nivel, posgrado.Nombre, ct);

        if (tieneTesis)
        {
            // Se da de baja para preservar la integridad de las tesis ya publicadas
            posgrado.Activo = false;
        }
        else
        {
            // Si no tiene tesis asociadas, se puede eliminar de la base de datos
            await _posgrados.EliminarAsync(posgrado, ct);
        }

        // 3. Quitar de los posgrados que dan clases los profesores
        await _profesores.RemoverPosgradoDeProfesoresAsync(posgrado.Nivel, posgrado.Nombre, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        var mensaje = tieneTesis
            ? "El posgrado tiene tesis registradas o publicadas, por lo que fue dado de baja para no afectar su historial. Se removió de los posgrados asignados a los profesores."
            : "El posgrado fue eliminado y removido de los posgrados asignados a los profesores.";

        return Ok(new ResultadoPosgradoAccionDto(mensaje, tieneTesis ? MapearConsulta.Posgrado(posgrado) : null));
    }
}

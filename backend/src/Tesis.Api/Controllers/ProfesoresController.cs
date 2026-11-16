using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Tesis.Api.Core;
using Tesis.Api.Core.Servicios;
using Tesis.Api.Modelos.Consultas;
using Tesis.Application.Abstracciones;
using Tesis.Application.Excepciones;
using Tesis.Domain.Entities;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/profesores")]
public class ProfesoresController : ControllerBase
{
    private readonly IProfesorRepository _profesores;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<Usuario> _userManager;
    private readonly ICorreoService _correo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ProfesoresController> _logger;

    public ProfesoresController(
        IProfesorRepository profesores,
        IUnitOfWork unitOfWork,
        UserManager<Usuario> userManager,
        ICorreoService correo,
        IConfiguration configuration,
        ILogger<ProfesoresController> logger)
    {
        _profesores = profesores;
        _unitOfWork = unitOfWork;
        _userManager = userManager;
        _correo = correo;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        var lista = await _profesores.ObtenerActivosAsync(ct);
        var dto = lista.Select(MapearConsulta.Profesor);
        return Ok(dto);
    }

    [HttpGet("yo")]
    [Authorize]
    public async Task<IActionResult> ObtenerMiPerfil(CancellationToken ct)
    {
        var profesor = await _profesores.ObtenerPorIdUsuarioAsync(IdUsuarioActual(), ct);
        if (profesor is null)
        {
            return NotFound(new { code = "PERFIL_NO_ENCONTRADO", message = "El usuario no tiene perfil de docente.", details = (string[]?)null });
        }

        return Ok(MapearConsulta.Profesor(profesor));
    }

    [HttpPost]
    [Authorize(Roles = $"{ConstantesRol.Docente},{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> Registrar(SolicitudProfesorRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            throw new SolicitudInvalidaException("El nombre es obligatorio.");
        }

        if (request.Posgrados is null || request.Posgrados.Count == 0)
        {
            throw new SolicitudInvalidaException("Debe indicar al menos un programa de posgrado.");
        }

        foreach (var posgrado in request.Posgrados)
        {
            if (string.IsNullOrWhiteSpace(posgrado.Programa))
            {
                throw new SolicitudInvalidaException("El programa de posgrado es obligatorio.");
            }
        }

        if (request.Posgrados.Select(p => (p.Nivel, p.Programa.Trim())).Distinct().Count() != request.Posgrados.Count)
        {
            throw new SolicitudInvalidaException("No se permiten programas de posgrado duplicados.");
        }

        var idUsuario = IdUsuarioActual();
        int? idUsuarioAsociado = null;
        Usuario? usuarioDocente = null;

        if (User.IsInRole(ConstantesRol.Docente))
        {
            if (await _profesores.ObtenerPorIdUsuarioAsync(idUsuario, ct) is not null)
            {
                throw new ConflictoDeNegocioException("El usuario ya tiene perfil de docente.");
            }

            idUsuarioAsociado = idUsuario;
        }
        else if (!string.IsNullOrWhiteSpace(request.Correo))
        {
            var correo = request.Correo.Trim();
            var dominioInstitucional = _configuration["Identity:EmailDominioInstitucional"] ?? "upt.edu.mx";

            if (!correo.EndsWith("@" + dominioInstitucional, StringComparison.OrdinalIgnoreCase))
            {
                throw new SolicitudInvalidaException($"El correo del docente debe pertenecer al dominio institucional @{dominioInstitucional}.");
            }

            usuarioDocente = await _userManager.FindByEmailAsync(correo);
            if (usuarioDocente is null)
            {
                usuarioDocente = new Usuario
                {
                    UserName = correo,
                    Email = correo,
                    EmailConfirmed = false,
                };
                var tempPass = $"Invitacion{Guid.NewGuid():N}!1Aa";
                var creacion = await _userManager.CreateAsync(usuarioDocente, tempPass);
                if (!creacion.Succeeded)
                {
                    throw new SolicitudInvalidaException("No se pudo crear la cuenta de invitación para el docente.");
                }

                await _userManager.AddToRoleAsync(usuarioDocente, ConstantesRol.Docente);
            }
            else
            {
                if (await _userManager.IsInRoleAsync(usuarioDocente, ConstantesRol.Alumno))
                {
                    throw new ConflictoDeNegocioException("Este correo institucional pertenece a una cuenta de alumno.");
                }

                if (!await _userManager.IsInRoleAsync(usuarioDocente, ConstantesRol.Docente))
                {
                    await _userManager.AddToRoleAsync(usuarioDocente, ConstantesRol.Docente);
                }

                if (await _profesores.ObtenerPorIdUsuarioAsync(usuarioDocente.Id, ct) is not null)
                {
                    throw new ConflictoDeNegocioException("Ya existe un profesor registrado con este correo institucional.");
                }
            }

            idUsuarioAsociado = usuarioDocente.Id;
        }

        var profesor = new Profesor
        {
            IdUsuario = idUsuarioAsociado,
            Usuario = usuarioDocente,
            Nombre = request.Nombre.Trim(),
            Activo = true,
        };

        foreach (var posgrado in request.Posgrados)
        {
            profesor.ProgramasPosgrado.Add(new ProfesorPosgrado
            {
                Nivel = posgrado.Nivel,
                Programa = posgrado.Programa.Trim(),
            });
        }

        await _profesores.AgregarAsync(profesor, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        if (usuarioDocente is not null && !string.IsNullOrWhiteSpace(usuarioDocente.Email))
        {
            try
            {
                var tokenInvitacion = await _userManager.GeneratePasswordResetTokenAsync(usuarioDocente);
                var enlace = $"{BaseFrontend()}/registro?invitacion={Uri.EscapeDataString(tokenInvitacion)}&id={usuarioDocente.Id}&correo={Uri.EscapeDataString(usuarioDocente.Email)}&nombre={Uri.EscapeDataString(profesor.Nombre)}";
                var cuerpoHtml = PlantillasCorreo.GenerarHtmlInvitacionDocente(profesor.Nombre, usuarioDocente.Email, enlace);
                await _correo.EnviarAsync(usuarioDocente.Email, "SIRET - UPT: Invitación para registrarte como Docente", cuerpoHtml, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar correo de invitación de docente a {Correo}.", usuarioDocente.Email);
            }
        }

        return StatusCode(StatusCodes.Status201Created, MapearConsulta.Profesor(profesor));
    }

    [HttpPut("{idProfesor:int}/posgrados")]
    [Authorize(Roles = $"{ConstantesRol.Administrador},{ConstantesRol.AdministradorDeTi}")]
    public async Task<IActionResult> ModificarPosgrados(int idProfesor, ActualizarPosgradosProfesorRequest request, CancellationToken ct)
    {
        var profesor = await _profesores.ObtenerConPosgradosAsync(idProfesor, ct)
            ?? throw new EntidadNoEncontradaException($"No existe el profesor con id {idProfesor}.");

        if (request.Posgrados is null || request.Posgrados.Count == 0)
        {
            throw new SolicitudInvalidaException("Debe indicar al menos un programa de posgrado para el profesor.");
        }

        foreach (var posgrado in request.Posgrados)
        {
            if (string.IsNullOrWhiteSpace(posgrado.Programa))
            {
                throw new SolicitudInvalidaException("El programa de posgrado no puede estar vacío.");
            }
        }

        if (request.Posgrados.Select(p => (p.Nivel, p.Programa.Trim().ToLower())).Distinct().Count() != request.Posgrados.Count)
        {
            throw new SolicitudInvalidaException("No se permiten programas de posgrado duplicados para el mismo profesor.");
        }

        profesor.ProgramasPosgrado.Clear();
        foreach (var posgrado in request.Posgrados)
        {
            profesor.ProgramasPosgrado.Add(new ProfesorPosgrado
            {
                IdProfesor = profesor.IdProfesor,
                Nivel = posgrado.Nivel,
                Programa = posgrado.Programa.Trim(),
            });
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return Ok(MapearConsulta.Profesor(profesor));
    }


    private string BaseFrontend()
    {
        if (Request?.Headers != null)
        {
            if (Request.Headers.TryGetValue("Origin", out var origin) && !string.IsNullOrWhiteSpace(origin))
            {
                return origin.ToString().TrimEnd('/');
            }
            if (Request.Headers.TryGetValue("Referer", out var referer) && !string.IsNullOrWhiteSpace(referer))
            {
                try
                {
                    var uri = new Uri(referer.ToString());
                    return $"{uri.Scheme}://{uri.Authority}".TrimEnd('/');
                }
                catch { }
            }
        }
        return _configuration["Url:BaseFrontend"] ?? "http://localhost:8081";
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
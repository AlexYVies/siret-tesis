using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tesis.Api.Core;
using Tesis.Api.Modelos.Consultas;
using Tesis.Api.Modelos.Tesis;
using Tesis.Application.Abstracciones;
using Tesis.Application.Excepciones;
using Tesis.Application.Modelos;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/tesis")]
public class TesisController : ControllerBase
{
    private readonly ITesisService _tesisService;
    private readonly ITesisRepository _tesisRepository;
    private readonly IAlumnoRepository _alumnos;
    private readonly IProfesorRepository _profesores;
    private readonly IAlmacenDocumentos _almacen;
    private readonly IConfiguration _configuration;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TesisController> _logger;

    public TesisController(
        ITesisService tesisService,
        ITesisRepository tesisRepository,
        IAlumnoRepository alumnos,
        IProfesorRepository profesores,
        IAlmacenDocumentos almacen,
        IConfiguration configuration,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TesisController> logger)
    {
        _tesisService = tesisService;
        _tesisRepository = tesisRepository;
        _alumnos = alumnos;
        _profesores = profesores;
        _almacen = almacen;
        _configuration = configuration;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Listar(CancellationToken ct)
    {
        if (EsPersonal())
        {
            var todas = await _tesisRepository.ObtenerTodasAsync(ct);
            return Ok(todas.Select(MapearConsulta.Listado));
        }

        if (User.IsInRole(ConstantesRol.Docente))
        {
            var profesor = await _profesores.ObtenerPorIdUsuarioAsync(IdUsuarioActual(), ct)
                ?? throw new OperacionNoPermitidaException("El usuario no tiene perfil de docente.");
            var misTesis = await _tesisRepository.ObtenerPorProfesorAsync(profesor.IdProfesor, ct);
            return Ok(misTesis.Select(MapearConsulta.Listado));
        }

        if (User.IsInRole(ConstantesRol.Alumno))
        {
            var alumno = await _alumnos.ObtenerPorIdUsuarioAsync(IdUsuarioActual(), ct)
                ?? throw new OperacionNoPermitidaException("El usuario no tiene perfil de alumno.");
            var misTesis = await _tesisRepository.ObtenerPorAlumnoAsync(alumno.IdAlumno, ct);
            return Ok(misTesis.Select(MapearConsulta.Listado));
        }

        if (User.IsInRole(ConstantesRol.Invitado))
        {
            var aceptadas = await _tesisRepository.ObtenerAceptadasAsync(
                new ConsultaRepositorio(null, null, null, null, OrdenRepositorio.Recientes, 1, 100),
                ct);
            return Ok(aceptadas.Datos.Select(MapearConsulta.Listado));
        }

        throw new OperacionNoPermitidaException("Su rol no permite consultar el listado de tesis.");
    }

    [HttpGet("{folio:guid}")]
    [Authorize]
    public async Task<IActionResult> Detalle(Guid folio, CancellationToken ct)
    {
        var tesis = await _tesisRepository.ObtenerPorFolioConNavegacionesAsync(folio, ct)
            ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

        if (!EsPersonal() && !await PuedeVerDetalleAsync(tesis, ct))
        {
            throw new OperacionNoPermitidaException("No tiene acceso a esta tesis.");
        }

        return Ok(MapearConsulta.Detalle(tesis));
    }

    private async Task<bool> PuedeVerDetalleAsync(Tesis.Domain.Entities.Tesis tesis, CancellationToken ct)
    {
        if (User.IsInRole(ConstantesRol.Invitado))
        {
            return tesis.Estado == TesisEstado.Aceptada;
        }

        if (User.IsInRole(ConstantesRol.Alumno))
        {
            var alumno = await _alumnos.ObtenerPorIdUsuarioAsync(IdUsuarioActual(), ct);
            return alumno is not null && alumno.IdAlumno == tesis.IdAlumno;
        }

        if (User.IsInRole(ConstantesRol.Docente))
        {
            var profesor = await _profesores.ObtenerPorIdUsuarioAsync(IdUsuarioActual(), ct);
            return profesor is not null &&
                   (tesis.IdAsesor == profesor.IdProfesor || tesis.IdCoAsesor == profesor.IdProfesor);
        }

        return false;
    }

    private bool EsPersonal() =>
        User.IsInRole(ConstantesRol.DirectorDePosgrado) ||
        User.IsInRole(ConstantesRol.AdministradorDeTi);

    [HttpPost]
    [Authorize(Roles = ConstantesRol.Alumno)]
    public async Task<IActionResult> Registrar(RegistrarTesisRequest request, CancellationToken ct)
    {
        var idAlumno = await ResolverIdAlumnoAsync(ct);
        var resultado = await _tesisService.RegistrarTesisAsync(
            new RegistrarTesisComando(
                request.Titulo,
                request.Nivel,
                request.Programa,
                request.IdAsesor,
                request.IdCoAsesor),
            idAlumno,
            ct);

        return StatusCode(StatusCodes.Status201Created, new { folio = resultado.Folio, estado = resultado.Estado });
    }

    [HttpPost("{folio:guid}/confirmaciones")]
    [Authorize(Roles = ConstantesRol.Docente)]
    public async Task<IActionResult> ProcesarConfirmacion(Guid folio, ProcesarConfirmacionRequest request, CancellationToken ct)
    {
        var idProfesor = await ResolverIdProfesorAsync(ct);
        await _tesisService.ProcesarConfirmacionAsync(
            new ProcesarConfirmacionComando(folio, request.Tipo, request.Decision),
            idProfesor,
            ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{folio:guid}/documento")]
    [Authorize(Roles = ConstantesRol.Alumno)]
    public async Task<IActionResult> SubirDocumento(Guid folio, IFormFile archivo, CancellationToken ct)
    {
        ValidarArchivo(archivo);
        var idAlumno = await ResolverIdAlumnoAsync(ct);

        await _tesisService.SubirDocumentoAsync(
            new SubirDocumentoComando(folio, archivo!.FileName),
            idAlumno,
            ct);

        var documento = await ObtenerUltimoDocumentoAsync(folio, ct);
        var nombreFisico = _almacen.GenerarNombreFisico(folio, documento.Version, documento.Nombre);
        await using var flujo = archivo.OpenReadStream();
        await _almacen.GuardarAsync(nombreFisico, flujo, ct);

        // Disparar la revisión por IA automáticamente en segundo plano para que esté lista para el profesor
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var servicio = scope.ServiceProvider.GetRequiredService<ITesisService>();
                await servicio.EjecutarRevisionIAAutomaticaAsync(folio);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al ejecutar la revisión IA automática para folio {Folio}", folio);
            }
        });

        return Ok(new { documento = nuevoDocumentoDto(documento) });
    }

    [HttpGet("{folio:guid}/documento")]
    [Authorize]
    public async Task<IActionResult> DescargarDocumento(Guid folio, [FromQuery] bool descargar = false, CancellationToken ct = default)
    {
        var tesis = await _tesisRepository.ObtenerPorFolioConNavegacionesAsync(folio, ct)
            ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

        if (!EsPersonal() && !await PuedeVerDetalleAsync(tesis, ct))
        {
            throw new OperacionNoPermitidaException("No tiene acceso a esta tesis.");
        }

        var documento = tesis.Documentos.OrderByDescending(d => d.Version).FirstOrDefault()
            ?? throw new EntidadNoEncontradaException("La tesis no tiene documento cargado.");

        var nombreFisico = _almacen.GenerarNombreFisico(folio, documento.Version, documento.Nombre);
        if (!_almacen.Existe(nombreFisico))
        {
            throw new EntidadNoEncontradaException("El archivo físico no está disponible.");
        }

        var flujo = _almacen.Abrir(nombreFisico);
        var tipoContenido = ContenidoArchivo(documento.Nombre);

        if (descargar)
        {
            return File(flujo, tipoContenido, documento.Nombre, enableRangeProcessing: true);
        }

        Response.Headers["Content-Disposition"] = $"inline; filename=\"{documento.Nombre}\"";
        return File(flujo, tipoContenido, enableRangeProcessing: true);
    }

    [HttpPost("{folio:guid}/articulos")]
    [Authorize(Roles = ConstantesRol.Alumno)]
    public async Task<IActionResult> AgregarArticulo(Guid folio, AgregarArticuloRequest request, CancellationToken ct)
    {
        var idAlumno = await ResolverIdAlumnoAsync(ct);
        await _tesisService.AgregarArticuloAsync(
            new AgregarArticuloComando(folio, request.Nombre, request.Url, request.Doi, request.IdTipoArticulo),
            idAlumno,
            ct);

        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("{folio:guid}/articulos/sugerencias-pdf")]
    [Authorize]
    public async Task<IActionResult> ObtenerSugerenciasPdf(Guid folio, CancellationToken ct)
    {
        var sugerencias = await _tesisService.ObtenerSugerenciasArticulosPdfAsync(folio, ct);
        return Ok(sugerencias);
    }

    [HttpPost("{folio:guid}/articulos/importar-referencias")]
    [Authorize]
    public async Task<IActionResult> ImportarReferencias(Guid folio, ImportarReferenciasRequest request, CancellationToken ct)
    {
        if (request?.Referencias is null || request.Referencias.Count == 0)
        {
            return BadRequest(new { mensaje = "No se proporcionaron referencias para importar." });
        }

        var idUsuario = IdUsuarioActual();
        var creados = await _tesisService.ImportarReferenciasArticulosAsync(folio, request.Referencias, idUsuario, ct);
        return Ok(creados.Select(MapearConsulta.Articulo));
    }

    [HttpPost("{folio:guid}/revision-ia")]
    [Authorize(Roles = ConstantesRol.Docente)]
    public async Task<IActionResult> RevisionIA(Guid folio, RegistrarRevisionIARequest request, CancellationToken ct)
    {
        var idProfesor = await ResolverIdProfesorAsync(ct);
        await _tesisService.ProcesarRevisionIAAsync(
            new RegistrarRevisionIAComando(folio, request.EsUltimaVersion),
            idProfesor,
            ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{folio:guid}/revision-asesor")]
    [Authorize(Roles = ConstantesRol.Docente)]
    public async Task<IActionResult> RevisionAsesor(Guid folio, ProcesarRevisionAsesorRequest request, CancellationToken ct)
    {
        var idProfesor = await ResolverIdProfesorAsync(ct);
        await _tesisService.ProcesarRevisionAsesorAsync(
            new ProcesarRevisionAsesorComando(folio, request.EsUltimaVersion, request.Decision, request.Observaciones),
            idProfesor,
            ct);

        return Ok(new { ok = true });
    }

    [HttpPost("{folio:guid}/dictamen")]
    [Authorize(Roles = ConstantesRol.DirectorDePosgrado)]
    public async Task<IActionResult> Dictamen(Guid folio, EmitirDictamenRequest request, CancellationToken ct)
    {
        var idDirector = IdUsuarioActual();
        await _tesisService.EmitirDictamenAsync(
            new EmitirDictamenComando(folio, request.Resultado, request.Observaciones),
            idDirector,
            ct);

        return Ok(new { ok = true });
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

    private async Task<int> ResolverIdAlumnoAsync(CancellationToken ct)
    {
        var alumno = await _alumnos.ObtenerPorIdUsuarioAsync(IdUsuarioActual(), ct)
            ?? throw new OperacionNoPermitidaException("El usuario no tiene perfil de alumno.");
        return alumno.IdAlumno;
    }

    private async Task<int> ResolverIdProfesorAsync(CancellationToken ct)
    {
        var profesor = await _profesores.ObtenerPorIdUsuarioAsync(IdUsuarioActual(), ct)
            ?? throw new OperacionNoPermitidaException("El usuario no tiene perfil de docente.");
        return profesor.IdProfesor;
    }

    private async Task<DocumentoTesis> ObtenerUltimoDocumentoAsync(Guid folio, CancellationToken ct)
    {
        var tesis = await _tesisRepository.ObtenerPorFolioConNavegacionesAsync(folio, ct)
            ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

        return tesis.Documentos.OrderByDescending(d => d.Version).First()
            ?? throw new OperacionNoPermitidaException("La tesis no tiene documento cargado.");
    }

    private static string ContenidoArchivo(string nombre) =>
        Path.GetExtension(nombre).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            _ => "application/octet-stream",
        };

    private static object nuevoDocumentoDto(DocumentoTesis documento) => new
    {
        idDocumento = documento.IdDocumento,
        folio = documento.Folio,
        nombre = documento.Nombre,
        version = documento.Version,
        fechaReg = documento.FechaReg,
    };

    private void ValidarArchivo(IFormFile archivo)
    {
        if (archivo is null || archivo.Length == 0)
        {
            throw new SolicitudInvalidaException("Debe adjuntar un documento.");
        }

        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var permitidas = _configuration["Almacenamiento:ExtensionesPermitidas"]
            ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            ?? new[] { ".pdf" };

        if (!permitidas.Contains(extension) || extension != ".pdf")
        {
            throw new SolicitudInvalidaException("Solo se permiten documentos de tesis en formato PDF (.pdf).");
        }

        var tamanoMaximo = _configuration.GetValue<long>("Almacenamiento:TamanoMaximoBytes");
        if (archivo.Length > tamanoMaximo)
        {
            throw new SolicitudInvalidaException("El archivo excede el tamaño máximo permitido.");
        }
    }
}
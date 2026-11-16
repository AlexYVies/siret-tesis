using Microsoft.AspNetCore.Mvc;
using Tesis.Api.Modelos.Consultas;
using Tesis.Application.Abstracciones;
using Tesis.Application.Excepciones;
using Tesis.Application.Modelos;
using Tesis.Domain.Enums;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/repositorio")]
public class RepositorioController : ControllerBase
{
    private readonly ITesisRepository _tesis;
    private readonly IAlmacenDocumentos _almacen;

    public RepositorioController(ITesisRepository tesis, IAlmacenDocumentos almacen)
    {
        _tesis = tesis;
        _almacen = almacen;
    }

    [HttpGet("{folio:guid}")]
    public async Task<IActionResult> Detalle(Guid folio, CancellationToken ct)
    {
        var tesis = await _tesis.ObtenerPorFolioConNavegacionesAsync(folio, ct)
            ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

        if (tesis.Estado != TesisEstado.Aceptada)
        {
            throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");
        }

        return Ok(MapearConsulta.Detalle(tesis));
    }

    [HttpGet("{folio:guid}/documento")]
    public async Task<IActionResult> DescargarDocumento(Guid folio, [FromQuery] bool descargar = false, CancellationToken ct = default)
    {
        var tesis = await _tesis.ObtenerPorFolioConNavegacionesAsync(folio, ct)
            ?? throw new EntidadNoEncontradaException($"No existe la tesis con folio '{folio}'.");

        if (tesis.Estado != TesisEstado.Aceptada)
        {
            throw new OperacionNoPermitidaException("La tesis no está disponible para consulta pública.");
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

    private static string ContenidoArchivo(string nombre) =>
        Path.GetExtension(nombre).ToLowerInvariant() switch
        {
            ".pdf" => "application/pdf",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".doc" => "application/msword",
            _ => "application/octet-stream",
        };

    [HttpGet]
    public async Task<IActionResult> Consultar(
        [FromQuery] string? q,
        [FromQuery] string? nivel,
        [FromQuery] string? programa,
        [FromQuery] string? autor,
        [FromQuery] string? orden,
        [FromQuery] int pagina = 1,
        [FromQuery] int tamano = 20,
        CancellationToken ct = default)
    {
        var resultado = await _tesis.ObtenerAceptadasAsync(
            new ConsultaRepositorio(
                q,
                ParsearNivel(nivel),
                programa,
                autor,
                ParsearOrden(orden),
                pagina,
                tamano),
            ct);

        return Ok(new RepositorioResultadoDto(
            resultado.Datos.Select(MapearConsulta.Repositorio).ToList(),
            resultado.Total,
            Math.Max(pagina, 1),
            Math.Clamp(tamano, 1, 100)));
    }

    private static NivelPosgrado? ParsearNivel(string? nivel) =>
        nivel?.Trim().ToLowerInvariant() switch
        {
            "maestria" or "maestría" => NivelPosgrado.Maestria,
            "doctorado" => NivelPosgrado.Doctorado,
            _ => null,
        };

    private static OrdenRepositorio ParsearOrden(string? orden) =>
        orden?.Trim().ToLowerInvariant() switch
        {
            "autor" => OrdenRepositorio.Autor,
            _ => OrdenRepositorio.Recientes,
        };
}
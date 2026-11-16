using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tesis.Api.Core;
using Tesis.Api.Modelos.Consultas;
using Tesis.Application.Abstracciones;
using Tesis.Domain.Enums;

namespace Tesis.Api.Controllers;

[ApiController]
[Route("api/indicadores")]
public class IndicadoresController : ControllerBase
{
    private readonly ITesisRepository _tesis;

    public IndicadoresController(ITesisRepository tesis)
    {
        _tesis = tesis;
    }

    [HttpGet]
    [Authorize(Roles = ConstantesRol.DirectorDePosgrado)]
    public async Task<IActionResult> Obtener(CancellationToken ct)
    {
        var todas = await _tesis.ObtenerTodasAsync(ct);
        var aceptadas = todas.Count(t => t.Estado == TesisEstado.Aceptada);
        var rechazadas = todas.Count(t => t.Estado == TesisEstado.Rechazada);

        var porPrograma = todas
            .GroupBy(t => new { t.Nivel, Programa = string.IsNullOrWhiteSpace(t.Programa) ? "Sin programa asignado" : t.Programa.Trim() })
            .Select(g => new IndicadorProgramaDto(
                g.Key.Programa,
                g.Key.Nivel,
                g.Count(),
                g.Count(t => t.Estado == TesisEstado.Aceptada),
                g.Count(t => t.Estado != TesisEstado.Aceptada && t.Estado != TesisEstado.Rechazada && t.Estado != TesisEstado.Inactiva),
                g.Count(t => t.Estado == TesisEstado.Rechazada)))
            .OrderByDescending(p => p.Total)
            .ToList();

        var porNivel = todas
            .GroupBy(t => t.Nivel)
            .Select(g => new IndicadorNivelDto(
                g.Key,
                g.Count(),
                g.Count(t => t.Estado == TesisEstado.Aceptada),
                g.Count(t => t.Estado != TesisEstado.Aceptada && t.Estado != TesisEstado.Rechazada && t.Estado != TesisEstado.Inactiva),
                g.Count(t => t.Estado == TesisEstado.Rechazada),
                porPrograma.Where(p => p.Nivel == g.Key).ToList()))
            .OrderBy(i => i.Nivel)
            .ToList();

        var porTipoArticulo = todas
            .Where(t => t.Estado == TesisEstado.Aceptada)
            .SelectMany(t => t.Articulos)
            .GroupBy(a => a.TipoArticulo?.Nombre ?? "Sin clasificar")
            .Select(g => new IndicadorTipoArticuloDto(g.Key, g.Count()))
            .OrderByDescending(i => i.Cantidad)
            .ToList();

        var dto = new IndicadoresDto(
            todas.Count,
            aceptadas,
            rechazadas,
            todas.Count - aceptadas - rechazadas,
            porNivel,
            porTipoArticulo,
            porPrograma);


        return Ok(dto);
    }
}
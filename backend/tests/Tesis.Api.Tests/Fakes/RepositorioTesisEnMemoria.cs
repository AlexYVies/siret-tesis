using System.Collections.Concurrent;
using Tesis.Application.Abstracciones;
using Tesis.Application.Modelos;
using Tesis.Domain.Enums;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Application.Tests.Fakes;

public sealed class RepositorioTesisEnMemoria : ITesisRepository
{
    private readonly ConcurrentDictionary<Guid, EntidadTesis> _tesis = new();

    public Task<EntidadTesis?> ObtenerPorFolioAsync(Guid folio, CancellationToken ct = default) =>
        Task.FromResult(_tesis.TryGetValue(folio, out var tesis) ? tesis : null);

    public Task<EntidadTesis?> ObtenerPorFolioConNavegacionesAsync(Guid folio, CancellationToken ct = default) =>
        Task.FromResult(_tesis.TryGetValue(folio, out var tesis) ? tesis : null);

    public Task<List<EntidadTesis>> ObtenerPorAlumnoAsync(int idAlumno, CancellationToken ct = default) =>
        Task.FromResult(_tesis.Values.Where(t => t.IdAlumno == idAlumno).ToList());

    public Task<List<EntidadTesis>> ObtenerPorProfesorAsync(int idProfesor, CancellationToken ct = default) =>
        Task.FromResult(_tesis.Values
            .Where(t => t.IdAsesor == idProfesor || t.IdCoAsesor == idProfesor)
            .ToList());

    public Task<List<EntidadTesis>> ObtenerTodasAsync(CancellationToken ct = default) =>
        Task.FromResult(_tesis.Values.ToList());

    public Task<ResultadoRepositorio> ObtenerAceptadasAsync(ConsultaRepositorio consulta, CancellationToken ct = default)
    {
        var resultado = _tesis.Values.Where(t => t.Estado == TesisEstado.Aceptada);

        if (!string.IsNullOrWhiteSpace(consulta.Termino))
        {
            var termino = consulta.Termino.Trim();
            resultado = resultado.Where(t =>
                t.Titulo.Contains(termino, StringComparison.OrdinalIgnoreCase) ||
                (t.Alumno?.Nombre.Contains(termino, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (consulta.Nivel is not null)
        {
            resultado = resultado.Where(t => t.Nivel == consulta.Nivel.Value);
        }

        if (!string.IsNullOrWhiteSpace(consulta.Programa))
        {
            resultado = resultado.Where(t =>
                t.Programa.Contains(consulta.Programa.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(consulta.Autor))
        {
            resultado = resultado.Where(t =>
                t.Alumno?.Nombre.Contains(consulta.Autor.Trim(), StringComparison.OrdinalIgnoreCase) ?? false);
        }

        resultado = consulta.Orden == OrdenRepositorio.Autor
            ? resultado.OrderBy(t => t.Alumno?.Nombre)
            : resultado.OrderByDescending(t => t.Dictamenes.DefaultIfEmpty().Max(d => d?.Fecha));

        var lista = resultado.ToList();
        var pagina = Math.Max(consulta.Pagina, 1);
        var tamano = Math.Clamp(consulta.Tamano, 1, 100);

        return Task.FromResult(new ResultadoRepositorio(
            lista.Skip((pagina - 1) * tamano).Take(tamano).ToList(),
            lista.Count));
    }

    public Task AgregarAsync(EntidadTesis tesis, CancellationToken ct = default)
    {
        if (tesis.Alumno == null)
        {
            tesis.Alumno = new Tesis.Domain.Entities.Alumno
            {
                IdAlumno = tesis.IdAlumno,
                Nombre = "María López",
                NumeroControl = "U2026001",
                Correo = "maria.lopez@upt.edu.mx"
            };
        }
        _tesis.TryAdd(tesis.Folio, tesis);
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(EntidadTesis tesis, CancellationToken ct = default)
    {
        _tesis[tesis.Folio] = tesis;
        return Task.CompletedTask;
    }

    public Task<bool> TieneTesisEnCursoAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default)
    {
        var estadosEnCurso = new[]
        {
            TesisEstado.Registrada,
            TesisEstado.Confirmada,
            TesisEstado.EnRevisionIA,
            TesisEstado.EnRevisionDelAsesor,
            TesisEstado.CambiosSolicitados,
            TesisEstado.EnDictamen,
        };

        var enCurso = _tesis.Values.Any(t =>
            t.Nivel == nivel &&
            t.Programa.Equals(programa, StringComparison.OrdinalIgnoreCase) &&
            estadosEnCurso.Contains(t.Estado));

        return Task.FromResult(enCurso);
    }

    public Task<bool> TieneTesisAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default)
    {
        var tiene = _tesis.Values.Any(t =>
            t.Nivel == nivel &&
            t.Programa.Equals(programa, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(tiene);
    }

    public Task ActualizarProgramaEnTesisAsync(
        NivelPosgrado nivelAnterior,
        string programaAnterior,
        NivelPosgrado nuevoNivel,
        string nuevoPrograma,
        CancellationToken ct = default)
    {
        foreach (var tesis in _tesis.Values)
        {
            if (tesis.Nivel == nivelAnterior && tesis.Programa.Equals(programaAnterior, StringComparison.OrdinalIgnoreCase))
            {
                tesis.Nivel = nuevoNivel;
                tesis.Programa = nuevoPrograma;
            }
        }
        return Task.CompletedTask;
    }


    public void Limpiar() => _tesis.Clear();
}
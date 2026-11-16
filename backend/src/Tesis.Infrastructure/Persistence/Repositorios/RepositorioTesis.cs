using Microsoft.EntityFrameworkCore;
using Tesis.Application.Abstracciones;
using Tesis.Application.Modelos;
using Tesis.Domain.Enums;
using Tesis.Infrastructure.Persistence;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioTesis : ITesisRepository
{
    private readonly TesisDbContext _contexto;

    public RepositorioTesis(TesisDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<EntidadTesis?> ObtenerPorFolioAsync(Guid folio, CancellationToken ct = default) =>
        await _contexto.Tesis
            .Include(t => t.Confirmaciones)
            .Include(t => t.Documentos)
            .Include(t => t.Articulos)
            .Include(t => t.RevisionesIA)
            .Include(t => t.RevisionesAsesor)
            .Include(t => t.Dictamenes)
            .FirstOrDefaultAsync(t => t.Folio == folio, ct);

    public async Task<EntidadTesis?> ObtenerPorFolioConNavegacionesAsync(Guid folio, CancellationToken ct = default) =>
        await ConsultaBase()
            .FirstOrDefaultAsync(t => t.Folio == folio, ct);

    public async Task<List<EntidadTesis>> ObtenerPorAlumnoAsync(int idAlumno, CancellationToken ct = default) =>
        await ConsultaBase()
            .Where(t => t.IdAlumno == idAlumno)
            .OrderByDescending(t => t.FechaReg)
            .ToListAsync(ct);

    public async Task<List<EntidadTesis>> ObtenerPorProfesorAsync(int idProfesor, CancellationToken ct = default) =>
        await ConsultaBase()
            .Where(t => t.IdAsesor == idProfesor || t.IdCoAsesor == idProfesor)
            .OrderByDescending(t => t.FechaReg)
            .ToListAsync(ct);

    public async Task<List<EntidadTesis>> ObtenerTodasAsync(CancellationToken ct = default) =>
        await ConsultaBase()
            .OrderByDescending(t => t.FechaReg)
            .ToListAsync(ct);

    public async Task<ResultadoRepositorio> ObtenerAceptadasAsync(ConsultaRepositorio consulta, CancellationToken ct = default)
    {
        var filas = _contexto.Tesis
            .Include(t => t.Alumno)
            .Include(t => t.Asesor)
            .Where(t => t.Estado == TesisEstado.Aceptada);

        if (!string.IsNullOrWhiteSpace(consulta.Termino))
        {
            var termino = consulta.Termino.Trim();
            filas = filas.Where(t =>
                EF.Functions.Like(t.Titulo, $"%{termino}%") ||
                EF.Functions.Like(t.Alumno!.Nombre, $"%{termino}%"));
        }

        if (consulta.Nivel is not null)
        {
            filas = filas.Where(t => t.Nivel == consulta.Nivel.Value);
        }

        if (!string.IsNullOrWhiteSpace(consulta.Programa))
        {
            var programa = consulta.Programa.Trim();
            filas = filas.Where(t => EF.Functions.Like(t.Programa, $"%{programa}%"));
        }

        if (!string.IsNullOrWhiteSpace(consulta.Autor))
        {
            var autor = consulta.Autor.Trim();
            filas = filas.Where(t => EF.Functions.Like(t.Alumno!.Nombre, $"%{autor}%"));
        }

        filas = consulta.Orden == OrdenRepositorio.Autor
            ? filas.OrderBy(t => t.Alumno!.Nombre)
            : filas.OrderByDescending(t => t.Dictamenes.Max(d => d.Fecha));

        var total = await filas.CountAsync(ct);
        var pagina = Math.Max(consulta.Pagina, 1);
        var tamano = Math.Clamp(consulta.Tamano, 1, 100);

        var datos = await filas
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync(ct);

        return new ResultadoRepositorio(datos, total);
    }

    public async Task AgregarAsync(EntidadTesis tesis, CancellationToken ct = default)
    {
        await _contexto.Tesis.AddAsync(tesis, ct);
    }

    public Task ActualizarAsync(EntidadTesis tesis, CancellationToken ct = default)
    {
        _contexto.Tesis.Update(tesis);
        return Task.CompletedTask;
    }

    public async Task<bool> TieneTesisEnCursoAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default)
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

        return await _contexto.Tesis.AnyAsync(t =>
            t.Nivel == nivel &&
            t.Programa == programa &&
            estadosEnCurso.Contains(t.Estado), ct);
    }

    public async Task<bool> TieneTesisAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default)
    {
        return await _contexto.Tesis.AnyAsync(t =>
            t.Nivel == nivel &&
            t.Programa == programa, ct);
    }

    public async Task ActualizarProgramaEnTesisAsync(
        NivelPosgrado nivelAnterior,
        string programaAnterior,
        NivelPosgrado nuevoNivel,
        string nuevoPrograma,
        CancellationToken ct = default)
    {
        var tesis = await _contexto.Tesis
            .Where(t => t.Nivel == nivelAnterior && t.Programa == programaAnterior)
            .ToListAsync(ct);

        foreach (var t in tesis)
        {
            t.Nivel = nuevoNivel;
            t.Programa = nuevoPrograma;
        }
    }


    private IQueryable<EntidadTesis> ConsultaBase() =>
        _contexto.Tesis
            .Include(t => t.Alumno)
            .Include(t => t.Asesor)
            .Include(t => t.CoAsesor)
            .Include(t => t.Confirmaciones)
            .Include(t => t.Documentos)
            .Include(t => t.Articulos).ThenInclude(a => a.TipoArticulo)
            .Include(t => t.RevisionesIA)
            .Include(t => t.RevisionesAsesor)
            .Include(t => t.Dictamenes);
}
using Microsoft.EntityFrameworkCore;
using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;
using Tesis.Infrastructure.Persistence;

namespace Tesis.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioProfesores : IProfesorRepository
{
    private readonly TesisDbContext _contexto;

    public RepositorioProfesores(TesisDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<Profesor?> ObtenerConPosgradosAsync(int idProfesor, CancellationToken ct = default) =>
        await _contexto.Profesores
            .Include(p => p.Usuario)
            .Include(p => p.ProgramasPosgrado)
            .FirstOrDefaultAsync(p => p.IdProfesor == idProfesor, ct);

    public Task<Profesor?> ObtenerPorIdUsuarioAsync(int idUsuario, CancellationToken ct = default) =>
        _contexto.Profesores
            .Include(p => p.Usuario)
            .Include(p => p.ProgramasPosgrado)
            .FirstOrDefaultAsync(p => p.IdUsuario == idUsuario, ct);

    public async Task<List<Profesor>> ObtenerActivosAsync(CancellationToken ct = default) =>
        await _contexto.Profesores
            .Include(p => p.Usuario)
            .Include(p => p.ProgramasPosgrado)
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync(ct);

    public async Task AgregarAsync(Profesor profesor, CancellationToken ct = default)
    {
        await _contexto.Profesores.AddAsync(profesor, ct);
    }

    public async Task RemoverPosgradoDeProfesoresAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default)
    {
        var registros = await _contexto.ProfesoresPosgrado
            .Where(pp => pp.Nivel == nivel && pp.Programa == programa)
            .ToListAsync(ct);

        if (registros.Count > 0)
        {
            _contexto.ProfesoresPosgrado.RemoveRange(registros);
        }
    }

    public async Task ActualizarProgramaEnProfesoresAsync(
        NivelPosgrado nivelAnterior,
        string programaAnterior,
        NivelPosgrado nuevoNivel,
        string nuevoPrograma,
        CancellationToken ct = default)
    {
        var registros = await _contexto.ProfesoresPosgrado
            .Where(pp => pp.Nivel == nivelAnterior && pp.Programa == programaAnterior)
            .ToListAsync(ct);

        foreach (var r in registros)
        {
            r.Nivel = nuevoNivel;
            r.Programa = nuevoPrograma;
        }
    }
}

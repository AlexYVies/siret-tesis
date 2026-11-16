using Microsoft.EntityFrameworkCore;
using Tesis.Application.Abstracciones;
using Tesis.Domain.Entities;
using Tesis.Infrastructure.Persistence;

namespace Tesis.Infrastructure.Persistence.Repositorios;

public sealed class RepositorioAlumnos : IAlumnoRepository
{
    private readonly TesisDbContext _contexto;

    public RepositorioAlumnos(TesisDbContext contexto)
    {
        _contexto = contexto;
    }

    public Task<Alumno?> ObtenerPorIdUsuarioAsync(int idUsuario, CancellationToken ct = default) =>
        _contexto.Alumnos.FirstOrDefaultAsync(a => a.IdUsuario == idUsuario, ct);

    public async Task AgregarAsync(Alumno alumno, CancellationToken ct = default)
    {
        await _contexto.Alumnos.AddAsync(alumno, ct);
    }
}
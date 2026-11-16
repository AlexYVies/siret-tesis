using Tesis.Domain.Entities;

namespace Tesis.Application.Abstracciones;

public interface IAlumnoRepository
{
    Task<Alumno?> ObtenerPorIdUsuarioAsync(int idUsuario, CancellationToken ct = default);
    Task AgregarAsync(Alumno alumno, CancellationToken ct = default);
}
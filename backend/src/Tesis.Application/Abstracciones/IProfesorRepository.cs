using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

namespace Tesis.Application.Abstracciones;

public interface IProfesorRepository
{
    Task<Profesor?> ObtenerConPosgradosAsync(int idProfesor, CancellationToken ct = default);
    Task<Profesor?> ObtenerPorIdUsuarioAsync(int idUsuario, CancellationToken ct = default);
    Task<List<Profesor>> ObtenerActivosAsync(CancellationToken ct = default);
    Task AgregarAsync(Profesor profesor, CancellationToken ct = default);
    Task RemoverPosgradoDeProfesoresAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default);
    Task ActualizarProgramaEnProfesoresAsync(NivelPosgrado nivelAnterior, string programaAnterior, NivelPosgrado nuevoNivel, string nuevoPrograma, CancellationToken ct = default);
}

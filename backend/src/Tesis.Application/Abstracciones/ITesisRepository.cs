using Tesis.Application.Modelos;
using Tesis.Domain.Entities;
using Tesis.Domain.Enums;

using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Application.Abstracciones;

public interface ITesisRepository
{
    Task<EntidadTesis?> ObtenerPorFolioAsync(Guid folio, CancellationToken ct = default);
    Task<EntidadTesis?> ObtenerPorFolioConNavegacionesAsync(Guid folio, CancellationToken ct = default);
    Task<List<EntidadTesis>> ObtenerPorAlumnoAsync(int idAlumno, CancellationToken ct = default);
    Task<List<EntidadTesis>> ObtenerPorProfesorAsync(int idProfesor, CancellationToken ct = default);
    Task<List<EntidadTesis>> ObtenerTodasAsync(CancellationToken ct = default);
    Task<ResultadoRepositorio> ObtenerAceptadasAsync(ConsultaRepositorio consulta, CancellationToken ct = default);
    Task AgregarAsync(EntidadTesis tesis, CancellationToken ct = default);
    Task ActualizarAsync(EntidadTesis tesis, CancellationToken ct = default);
    Task<bool> TieneTesisEnCursoAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default);
    Task<bool> TieneTesisAsync(NivelPosgrado nivel, string programa, CancellationToken ct = default);
    Task ActualizarProgramaEnTesisAsync(NivelPosgrado nivelAnterior, string programaAnterior, NivelPosgrado nuevoNivel, string nuevoPrograma, CancellationToken ct = default);
}
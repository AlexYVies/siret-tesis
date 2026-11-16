using Tesis.Application.Modelos;

namespace Tesis.Application.Abstracciones;

public interface IServicioRevisionIA
{
    Task<RevisionIAResultado> RevisarAsync(Guid folio, int version, CancellationToken ct = default);
}
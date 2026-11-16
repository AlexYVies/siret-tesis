using Tesis.Application.Abstracciones;
using Tesis.Application.Modelos;
using Tesis.Domain.Enums;

namespace Tesis.Application.Tests.Fakes;

public sealed class ServicioRevisionIAEstable : IServicioRevisionIA
{
    public Task<RevisionIAResultado> RevisarAsync(Guid folio, int version, CancellationToken ct = default) =>
        Task.FromResult(new RevisionIAResultado(ResultadoRevisionIA.Valido, 95m));
}
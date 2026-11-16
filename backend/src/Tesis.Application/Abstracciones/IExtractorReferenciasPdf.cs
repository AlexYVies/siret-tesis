using Tesis.Application.Modelos;
using EntidadTesis = Tesis.Domain.Entities.Tesis;

namespace Tesis.Application.Abstracciones;

public interface IExtractorReferenciasPdf
{
    Task<IReadOnlyList<ReferenciaDetectadaDto>> ExtraerAsync(
        EntidadTesis tesis,
        CancellationToken ct = default);
}

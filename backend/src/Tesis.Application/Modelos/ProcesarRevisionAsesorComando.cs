using Tesis.Domain.Enums;

namespace Tesis.Application.Modelos;

public record ProcesarRevisionAsesorComando(
    Guid Folio,
    bool EsUltimaVersion,
    DecisionRevisionAsesor Decision,
    string? Observaciones);
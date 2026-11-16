using Tesis.Domain.Enums;

namespace Tesis.Api.Modelos.Tesis;

public record ProcesarRevisionAsesorRequest(
    bool EsUltimaVersion,
    DecisionRevisionAsesor Decision,
    string? Observaciones);
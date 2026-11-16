using Tesis.Domain.Enums;

namespace Tesis.Application.Modelos;

public record RevisionIAResultado(ResultadoRevisionIA Resultado, decimal Porcentaje, string? ReporteJson = null);
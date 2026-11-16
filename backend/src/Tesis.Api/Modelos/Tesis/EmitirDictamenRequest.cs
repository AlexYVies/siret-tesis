using Tesis.Domain.Enums;

namespace Tesis.Api.Modelos.Tesis;

public record EmitirDictamenRequest(ResultadoDictamen Resultado, string? Observaciones);
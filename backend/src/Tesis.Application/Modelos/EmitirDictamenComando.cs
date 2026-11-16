using Tesis.Domain.Enums;

namespace Tesis.Application.Modelos;

public record EmitirDictamenComando(Guid Folio, ResultadoDictamen Resultado, string? Observaciones);